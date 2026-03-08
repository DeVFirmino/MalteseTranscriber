using MalteseTranscriber.Core.Interfaces;
using MalteseTranscriber.Core.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace MalteseTranscriber.Infrastructure;

public class TranscriptionPipeline : ITranscriptionPipeline
{
    private readonly IStreamingTranscriptionService _transcription;
    private readonly ITranslationService _translator;
    private readonly ITranscriptionNotifier _notifier;
    private readonly IMemoryCache _cache;
    private readonly ILogger<TranscriptionPipeline> _logger;

    /// <summary>
    /// Tracks latest chunk index per session for notification ordering.
    /// </summary>
    private readonly Dictionary<string, int> _chunkIndex = new();

    public TranscriptionPipeline(
        IStreamingTranscriptionService transcription,
        ITranslationService translator,
        ITranscriptionNotifier notifier,
        IMemoryCache cache,
        ILogger<TranscriptionPipeline> logger)
    {
        _transcription = transcription;
        _translator = translator;
        _notifier = notifier;
        _cache = cache;
        _logger = logger;
    }

    public async Task InitializeSessionAsync(string sessionId, string connectionId)
    {
        var session = new TranscriptionSession
        {
            SessionId = sessionId,
            ConnectionId = connectionId
        };
        _cache.Set($"session:{sessionId}", session, TimeSpan.FromHours(2));
        _chunkIndex[sessionId] = 0;

        // Open persistent WebSocket to Speechmatics for this session
        await _transcription.ConnectAsync(sessionId, OnTranscriptReceivedAsync);

        _logger.LogInformation("Session initialized: {SessionId}", sessionId);
    }

    public async Task ProcessChunkAsync(string sessionId, byte[] audioBytes, int chunkIndex)
    {
        var session = _cache.Get<TranscriptionSession>($"session:{sessionId}");
        if (session is null)
        {
            _logger.LogWarning("Session not found: {SessionId}", sessionId);
            return;
        }

        _chunkIndex[sessionId] = chunkIndex;

        // Forward raw PCM directly — no buffering, no WAV conversion.
        // Speechmatics handles its own audio context internally.
        await _transcription.SendAudioAsync(sessionId, audioBytes);
    }

    /// <summary>
    /// Callback invoked by the streaming transcription service when a
    /// finalized transcript arrives. Triggers translation and notification.
    /// </summary>
    private async Task OnTranscriptReceivedAsync(string sessionId, string malteseText)
    {
        var chunkIndex = _chunkIndex.GetValueOrDefault(sessionId, 0);

        try
        {
            _logger.LogInformation("[{Chunk}] Maltese: {Text}", chunkIndex, malteseText);
            await _notifier.SendMalteseTranscriptionAsync(sessionId, chunkIndex, malteseText);

            var english = await _translator.TranslateAsync(malteseText, sessionId);
            _logger.LogInformation("[{Chunk}] English: {Text}", chunkIndex, english);
            await _notifier.SendEnglishTranslationAsync(sessionId, chunkIndex, malteseText, english);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in transcript callback for session {SessionId}", sessionId);
            await _notifier.SendErrorAsync(sessionId, chunkIndex, ex.Message);
        }
    }

    public async Task FinalizeSessionAsync(string sessionId)
    {
        await _transcription.DisconnectAsync(sessionId);
        _cache.Remove($"session:{sessionId}");
        _chunkIndex.Remove(sessionId);
        _logger.LogInformation("Session finalized: {SessionId}", sessionId);
    }

    public Task CleanupConnectionAsync(string connectionId)
    {
        return Task.CompletedTask;
    }
}
