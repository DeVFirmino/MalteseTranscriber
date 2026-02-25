using MalteseTranscriber.Core.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace MalteseTranscriber.API.Hubs;

public class TranscriptionHub : Hub
{
    private readonly ITranscriptionPipeline _pipeline;
    private readonly ILogger<TranscriptionHub> _logger;

    public TranscriptionHub(
        ITranscriptionPipeline pipeline,
        ILogger<TranscriptionHub> logger)
    {
        _pipeline = pipeline;
        _logger = logger;
    }

    public async Task StartSession(string sessionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);
        await _pipeline.InitializeSessionAsync(sessionId, Context.ConnectionId);
        _logger.LogInformation("Hub: session started {SessionId}", sessionId);
    }

    public async Task SendAudioChunk(string sessionId, string audioBase64, int chunkIndex)
    {
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
