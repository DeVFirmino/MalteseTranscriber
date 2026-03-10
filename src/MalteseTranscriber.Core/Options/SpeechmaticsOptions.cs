namespace MalteseTranscriber.Core.Options;

public class SpeechmaticsOptions
{
    public string Endpoint { get; set; } = "wss://eu.rt.speechmatics.com/v2";
    public string ApiKey { get; set; } = string.Empty;
    public int DisconnectDelayMs { get; set; } = 2000;
    public int MaxConcurrentSessions { get; set; } = 10;
}
