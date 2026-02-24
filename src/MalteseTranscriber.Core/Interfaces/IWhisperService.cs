namespace MalteseTranscriber.Core.Interfaces;

public interface IWhisperService
{
    Task<string> TranscribeAsync(byte[] wavBytes, string language = "mt");
}
