using MalteseTranscriber.Core.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using System.Text;
using System.Text.Json;

namespace MalteseTranscriber.Infrastructure;

public class TranslationService : ITranslationService
{
    private readonly HttpClient _http;
    private readonly IMemoryCache _cache;

    public TranslationService(HttpClient http, IMemoryCache cache)
    {
        _http = http;
        _cache = cache;
    }

    public async Task<string> TranslateAsync(string malteseText, string sessionId)
    {
        // Keep last 5 phrases as context so translation stays coherent
        var history = _cache.GetOrCreate($"ctx:{sessionId}", e =>
        {
            e.SlidingExpiration = TimeSpan.FromHours(1);
            return new Queue<string>();
        })!;

        var context = string.Join(" | ", history.TakeLast(5));

        var body = new
        {
            model = "gpt-4o",
            max_tokens = 300,
            messages = new[]
            {
                new
                {
                    role = "system",
                    content = "You are a real-time Maltese to English translator. " +
                              "Output ONLY the English translation. Nothing else."
                },
                new
                {
                    role = "user",
                    content = $"Context (do not translate): {context}\n\n" +
                              $"Translate to English: {malteseText}"
                }
            }
        };

        var response = await _http.PostAsync(
            "https://api.openai.com/v1/chat/completions",
            new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"));

        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var translation = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;

        // Update context
        history.Enqueue(malteseText);
        if (history.Count > 10) history.Dequeue();
        _cache.Set($"ctx:{sessionId}", history, TimeSpan.FromHours(1));

        return translation.Trim();
    }
}
