using MalteseTranscriber.Core.Interfaces;
using MalteseTranscriber.Core.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace MalteseTranscriber.Infrastructure;

public class TranscriptionPipeline : ITranscriptionPipeline
{
    private readonly IWhisperService _whisper;
    private readonly ITranslationService _translator;
    private readonly ITranscriptionNotifier _notifier;
    private readonly IMemoryCache _cache;
    private readonly ILogger<TranscriptionPipeline> _logger;

    public TranscriptionPipeline(
        IWhisperService whisper,
        ITranslationService translator,
        ITranscriptionNotifier notifier,
        IMemoryCache cache,
        ILogger<TranscriptionPipeline> logger)
    {
        _whisper = whisper;
        _translator = translator;
        _notifier = notifier;
        _cache = cache;
        _logger = logger;
    }

    public Task InitializeSessionAsync(string sessionId, string connectionId)
    {
        var session = new TranscriptionSession
        {
            SessionId = sessionId,
            ConnectionId = connectionId
        };
        _cache.Set($"session:{sessionId}", session, TimeSpan.FromHours(2));
        _logger.LogInformation("Session initialized: {SessionId}", sessionId);
        return Task.CompletedTask;
    }

    public async Task ProcessChunkAsync(string sessionId, byte[] audioBytes, int chunkIndex)
    {
        var session = _cache.Get<TranscriptionSession>($"session:{sessionId}");
        if (session is null)
        {
            _logger.LogWarning("Session not found: {SessionId}", sessionId);
            return;
        }

        session.AudioBuffer.AddRange(audioBytes);

        if (session.AudioBuffer.Count < session.MinBytesForProcessing) return;

        var audioToProcess = session.OverlapBuffer
            .Concat(session.AudioBuffer.Take(session.MinBytesForProcessing))
            .ToArray();

        session.OverlapBuffer = audioToProcess
            .TakeLast(session.OverlapBytes)
            .ToArray();

        session.AudioBuffer.RemoveRange(0, session.MinBytesForProcessing);

        _ = Task.Run(() => TranscribeAndTranslateAsync(sessionId, audioToProcess, chunkIndex));
    }

    private async Task TranscribeAndTranslateAsync(
        string sessionId, byte[] audio, int chunkIndex)
    {
        try
        {
            var wav = AudioConverter.PcmToWav(audio);

            var maltese = await _whisper.TranscribeAsync(wav, "mt");
            if (string.IsNullOrWhiteSpace(maltese)) return;

            _logger.LogInformation("[{Chunk}] Maltese: {Text}", chunkIndex, maltese);

            await _notifier.SendMalteseTranscriptionAsync(sessionId, chunkIndex, maltese);

            var english = await _translator.TranslateAsync(maltese, sessionId);

            _logger.LogInformation("[{Chunk}] English: {Text}", chunkIndex, english);

            await _notifier.SendEnglishTranslationAsync(sessionId, chunkIndex, maltese, english);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chunk {ChunkIndex}", chunkIndex);
            await _notifier.SendErrorAsync(sessionId, chunkIndex, ex.Message);
        }
    }

    public Task FinalizeSessionAsync(string sessionId)
    {
        _cache.Remove($"session:{sessionId}");
        _logger.LogInformation("Session finalized: {SessionId}", sessionId);
        return Task.CompletedTask;
    }

    public Task CleanupConnectionAsync(string connectionId)
    {
        return Task.CompletedTask;
    }
}
