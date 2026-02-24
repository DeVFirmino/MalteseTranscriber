using MalteseTranscriber.Core.Interfaces;

namespace MalteseTranscriber.Infrastructure;

public class WhisperService : IWhisperService
{
    private readonly HttpClient _http;

    public WhisperService(HttpClient http) => _http = http;

    public async Task<string> TranscribeAsync(byte[] wavBytes, string language = "mt")
    {
        using var content = new MultipartFormDataContent();

        content.Add(new ByteArrayContent(wavBytes), "file", "audio.wav");
        content.Add(new StringContent("whisper-1"), "model");
        content.Add(new StringContent(language), "language");
        content.Add(new StringContent("text"), "response_format");

        // This prompt hint significantly improves Maltese character accuracy
        content.Add(
            new StringContent("Maltese transcription. Include proper characters: ħ, għ, ċ, ż."),
            "prompt");

        var response = await _http.PostAsync(
            "https://api.openai.com/v1/audio/transcriptions", content);

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadAsStringAsync()).Trim();
    }
}
