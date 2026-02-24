namespace MalteseTranscriber.Core.Interfaces;

public interface ITranslationService
{
    Task<string> TranslateAsync(string malteseText, string sessionId);
}
