namespace MalteseTranscriber.Core.Options;

public class TranslationOptions
{
    public string Model { get; set; } = "gpt-4o";
    public int MaxTokens { get; set; } = 300;
    public int ContextWindowSize { get; set; } = 5;
    public string BaseUrl { get; set; } = "https://api.openai.com/v1/chat/completions";
}
