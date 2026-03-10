namespace MalteseTranscriber.Core.Interfaces;

/// <summary>
/// Streaming transcription service that maintains a persistent connection per session.
/// Audio is streamed in continuously; transcription results arrive via callback.
/// </summary>
public interface IStreamingTranscriptionService
{
    /// <summary>
    /// Opens a connection for the given session. Must be called before SendAudioAsync.
    /// </summary>
    /// <param name="sessionId">Unique session identifier.</param>
    /// <param name="onTranscript">Callback invoked for each finalized transcript: (sessionId, text).</param>
    Task ConnectAsync(string sessionId, Func<string, string, Task> onTranscript);

    /// <summary>
    /// Sends raw PCM audio bytes to the transcription engine.
    /// Audio format: 16kHz, 16-bit signed LE, mono.
    /// </summary>
    Task SendAudioAsync(string sessionId, byte[] pcmBytes);

    /// <summary>
    /// Gracefully closes the connection for the given session.
    /// </summary>
    Task DisconnectAsync(string sessionId);
}
