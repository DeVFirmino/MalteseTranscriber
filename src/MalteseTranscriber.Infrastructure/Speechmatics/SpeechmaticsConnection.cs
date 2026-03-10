using System.Net.WebSockets;

namespace MalteseTranscriber.Infrastructure.Speechmatics;

/// <summary>
/// Holds the state of a single Speechmatics WebSocket session:
/// the socket itself, the transcript callback, and concurrency controls.
/// </summary>
internal sealed class SpeechmaticsConnection : IDisposable
{
    public ClientWebSocket WebSocket { get; }
    public Func<string, string, Task> OnTranscript { get; }
    public CancellationTokenSource CancellationSource { get; }
    public SemaphoreSlim SendSemaphore { get; } = new(1, 1);
    public int SequenceNumber { get; set; }

    public SpeechmaticsConnection(
        ClientWebSocket ws,
        Func<string, string, Task> onTranscript,
        CancellationTokenSource cts)
    {
        WebSocket = ws;
        OnTranscript = onTranscript;
        CancellationSource = cts;
    }

    public void Dispose()
    {
        SendSemaphore.Dispose();
        CancellationSource.Dispose();
        WebSocket.Dispose();
    }
}
