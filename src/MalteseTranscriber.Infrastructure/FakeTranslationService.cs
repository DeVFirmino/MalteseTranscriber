using MalteseTranscriber.Core.Interfaces;

namespace MalteseTranscriber.Infrastructure;

public class FakeTranslationService : ITranslationService
{
    private static readonly Dictionary<string, string> Translations = new()
    {
        ["Bongu, kif int?"] = "Good morning, how are you?",
        ["Grazzi ħafna għal kollox."] = "Thank you very much for everything.",
        ["Fejn tista' tgħinni llum?"] = "Where can you help me today?",
        ["Il-maltese huwa lingwa unika."] = "Maltese is a unique language.",
        ["Nifhem ftit bil-Malti."] = "I understand a little Maltese.",
    };

    public Task<string> TranslateAsync(string malteseText, string sessionId)
    {
        var english = Translations.GetValueOrDefault(malteseText, $"[Translation of: {malteseText}]");
        return Task.FromResult(english);
    }
}
