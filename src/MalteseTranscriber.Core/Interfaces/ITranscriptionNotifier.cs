namespace MalteseTranscriber.Core.Interfaces;

public interface ITranscriptionNotifier
{
    Task SendMalteseTranscriptionAsync(string sessionId, int chunkIndex, string text);
    Task SendEnglishTranslationAsync(string sessionId, int chunkIndex, string originalText, string translatedText);
    Task SendErrorAsync(string sessionId, int chunkIndex, string message);
}
