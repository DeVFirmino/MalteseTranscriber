using FluentValidation;
using MalteseTranscriber.Core.Interfaces;
using MalteseTranscriber.Core.Requests;
using Microsoft.AspNetCore.SignalR;

namespace MalteseTranscriber.API.Hubs;

public class TranscriptionHub : Hub
{
    private readonly ITranscriptionPipeline _pipeline;
    private readonly IValidator<StartSessionRequest> _sessionValidator;
    private readonly IValidator<AudioChunkRequest> _chunkValidator;
    private readonly ILogger<TranscriptionHub> _logger;

    public TranscriptionHub(
        ITranscriptionPipeline pipeline,
        IValidator<StartSessionRequest> sessionValidator,
        IValidator<AudioChunkRequest> chunkValidator,
        ILogger<TranscriptionHub> logger)
    {
        _pipeline = pipeline;
        _sessionValidator = sessionValidator;
        _chunkValidator = chunkValidator;
        _logger = logger;
    }

    public async Task StartSession(string sessionId)
    {
        var result = await _sessionValidator.ValidateAsync(new StartSessionRequest(sessionId));
        if (!result.IsValid)
        {
            await Clients.Caller.SendAsync("OnError", new
            {
                ChunkIndex = -1,
                Message = string.Join("; ", result.Errors.Select(e => e.ErrorMessage))
            });
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);
        await _pipeline.InitializeSessionAsync(sessionId, Context.ConnectionId);
        _logger.LogInformation("Hub: session started {SessionId}", sessionId);
    }

    public async Task SendAudioChunk(string sessionId, string audioBase64, int chunkIndex)
    {
        var result = await _chunkValidator.ValidateAsync(
            new AudioChunkRequest(sessionId, audioBase64, chunkIndex));

        if (!result.IsValid)
        {
            await Clients.Caller.SendAsync("OnError", new
            {
                ChunkIndex = chunkIndex,
                Message = string.Join("; ", result.Errors.Select(e => e.ErrorMessage))
            });
            return;
        }

        var bytes = Convert.FromBase64String(audioBase64);
        await _pipeline.ProcessChunkAsync(sessionId, bytes, chunkIndex);
    }

    public async Task EndSession(string sessionId)
    {
        await _pipeline.FinalizeSessionAsync(sessionId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, sessionId);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await _pipeline.CleanupConnectionAsync(Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}
