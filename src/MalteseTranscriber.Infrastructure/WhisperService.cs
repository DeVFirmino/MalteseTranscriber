using MalteseTranscriber.Core.Interfaces;

namespace MalteseTranscriber.Infrastructure;

public class WhisperService : IWhisperService
{
    private readonly HttpClient _http;

    public WhisperService(HttpClient http) => _http = http;

    public async Task<string> TranscribeAsync(byte[] wavBytes)
    {
        using var content = new MultipartFormDataContent();

        content.Add(new ByteArrayContent(wavBytes), "file", "audio.wav");
        content.Add(new StringContent("whisper-1"), "model");
        content.Add(new StringContent("text"), "response_format");

        // Prompt guides Whisper toward Maltese without the unsupported language param
        content.Add(
            new StringContent("This is Maltese (Malti) speech. Transcribe accurately including: ħ, għ, ċ, ż."),
            "prompt");

        var response = await _http.PostAsync(
            "https://api.openai.com/v1/audio/transcriptions", content);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"Whisper API {(int)response.StatusCode}: {errorBody}");
        }

        return (await response.Content.ReadAsStringAsync()).Trim();
    }
}
