using MalteseTranscriber.Core.Interfaces;
using MalteseTranscriber.Core.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace MalteseTranscriber.Infrastructure.Translation;

public class TranslationService : ITranslationService
{
    private readonly HttpClient _http;
    private readonly IMemoryCache _cache;
    private readonly TranslationOptions _options;

    public TranslationService(HttpClient http, IMemoryCache cache, IOptions<TranslationOptions> options)
    {
        _http = http;
        _cache = cache;
        _options = options.Value;
    }

    public async Task<string> TranslateAsync(string malteseText, string sessionId)
    {
        var history = _cache.GetOrCreate($"ctx:{sessionId}", e =>
        {
            e.SlidingExpiration = TimeSpan.FromHours(1);
            return new Queue<string>();
        })!;

        var context = string.Join(" | ", history.TakeLast(_options.ContextWindowSize));

        var body = new
        {
            model = _options.Model,
            max_tokens = _options.MaxTokens,
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
            _options.BaseUrl,
            new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"));

        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var translation = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;

        history.Enqueue(malteseText);
        if (history.Count > _options.ContextWindowSize)
            history.Dequeue();

        _cache.Set($"ctx:{sessionId}", history, TimeSpan.FromHours(1));

        return translation.Trim();
    }
}
