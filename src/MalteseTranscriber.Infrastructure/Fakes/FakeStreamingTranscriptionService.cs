using MalteseTranscriber.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace MalteseTranscriber.Infrastructure.Fakes;

/// <summary>
/// Fake streaming transcription for Development mode.
/// Simulates Speechmatics by returning Maltese phrases after accumulating audio.
/// </summary>
public class FakeStreamingTranscriptionService : IStreamingTranscriptionService
{
    private static readonly string[] Phrases =
    [
        "Bongu, kif int?",
        "Grazzi ħafna għal kollox.",
        "Fejn tista' tgħinni llum?",
        "Il-maltese huwa lingwa unika.",
        "Nifhem ftit bil-Malti.",
    ];

    private readonly Dictionary<string, SessionState> _sessions = new();
    private readonly ILogger<FakeStreamingTranscriptionService> _logger;

    public FakeStreamingTranscriptionService(ILogger<FakeStreamingTranscriptionService> logger)
    {
        _logger = logger;
    }

    public Task ConnectAsync(string sessionId, Func<string, string, Task> onTranscript)
    {
        _sessions[sessionId] = new SessionState(onTranscript);
        _logger.LogInformation("[Fake] Streaming transcription connected for {SessionId}", sessionId);
        return Task.CompletedTask;
    }

    public async Task SendAudioAsync(string sessionId, byte[] pcmBytes)
    {
        if (!_sessions.TryGetValue(sessionId, out var state))
            return;

        state.BytesReceived += pcmBytes.Length;

        // Simulate: produce a transcript every ~96KB (3 seconds of audio)
        if (state.BytesReceived >= 96_000)
        {
            state.BytesReceived = 0;
            await Task.Delay(800); // simulate transcription latency

            var phrase = Phrases[state.PhraseIndex++ % Phrases.Length];
            await state.OnTranscript(sessionId, phrase);
        }
    }

    public Task DisconnectAsync(string sessionId)
    {
        _sessions.Remove(sessionId);
        _logger.LogInformation("[Fake] Streaming transcription disconnected for {SessionId}", sessionId);
        return Task.CompletedTask;
    }

    private class SessionState
    {
        public Func<string, string, Task> OnTranscript { get; }
        public int BytesReceived { get; set; }
        public int PhraseIndex { get; set; }

        public SessionState(Func<string, string, Task> onTranscript)
        {
            OnTranscript = onTranscript;
        }
    }
}