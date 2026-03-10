namespace MalteseTranscriber.Infrastructure.Speechmatics.Models;

/// <summary>
/// Speechmatics WebSocket protocol messages for real-time transcription.
/// See: https://docs.speechmatics.com/api-ref/realtime-transcription-websocket
/// </summary>

public class StartRecognitionMessage
{
    public string Message { get; set; } = "StartRecognition";
    public AudioFormatConfig AudioFormat { get; set; } = new();
    public TranscriptionConfig TranscriptionConfig { get; set; } = new();
}

public class AudioFormatConfig
{
    public string Type { get; set; } = "raw";
    public string Encoding { get; set; } = "pcm_s16le";
    public int SampleRate { get; set; } = 16000;
}

public class TranscriptionConfig
{
    public string Language { get; set; } = "mt";
    public bool EnablePartials { get; set; } = false;
    public int MaxDelay { get; set; } = 2;
}

public class EndOfStreamMessage
{
    public string Message { get; set; } = "EndOfStream";
    public int LastSeqNo { get; set; }
}

public class SpeechmaticsResponse
{
    public string Message { get; set; } = string.Empty;
}

public class AddTranscriptResponse : SpeechmaticsResponse
{
    public TranscriptMetadata Metadata { get; set; } = new();
    public List<TranscriptResult> Results { get; set; } = new();
}

public class TranscriptMetadata
{
    public double StartTime { get; set; }
    public double EndTime { get; set; }
    public string Transcript { get; set; } = string.Empty;
}

public class TranscriptResult
{
    public string Type { get; set; } = string.Empty;
    public double StartTime { get; set; }
    public double EndTime { get; set; }
    public string Content { get; set; } = string.Empty;
}

public class SpeechmaticsErrorResponse : SpeechmaticsResponse
{
    public string Type { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}
