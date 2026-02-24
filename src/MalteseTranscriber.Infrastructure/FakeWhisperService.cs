using MalteseTranscriber.Core.Interfaces;

namespace MalteseTranscriber.Infrastructure;

public class FakeWhisperService : IWhisperService
{
    private static readonly string[] Phrases =
    [
        "Bongu, kif int?",
        "Grazzi ħafna għal kollox.",
        "Fejn tista' tgħinni llum?",
        "Il-maltese huwa lingwa unika.",
        "Nifhem ftit bil-Malti.",
    ];

    private int _index = 0;

    public async Task<string> TranscribeAsync(byte[] wavBytes, string language = "mt")
    {
        await Task.Delay(800); // simulate real API latency
        return Phrases[_index++ % Phrases.Length];
    }
}
