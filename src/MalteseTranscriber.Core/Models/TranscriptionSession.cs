namespace MalteseTranscriber.Core.Models;

public class TranscriptionSession
{
    public string SessionId { get; set; } = string.Empty;
    public string ConnectionId { get; set; } = string.Empty;
    public List<byte> AudioBuffer { get; set; } = new();
    public byte[] OverlapBuffer { get; set; } = Array.Empty<byte>();
    public Queue<string> ContextHistory { get; set; } = new();
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // 16kHz × 1 channel × 2 bytes (Int16) × 3 seconds = 96,000 bytes
    public int MinBytesForProcessing => 96_000;

    // 500ms of overlap to avoid losing words at chunk boundaries
    public int OverlapBytes => 16_000;
}
