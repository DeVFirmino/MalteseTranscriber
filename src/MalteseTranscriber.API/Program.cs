using dotenv.net;
using MalteseTranscriber.Core.Interfaces;
using MalteseTranscriber.Infrastructure;

// Load .env from project root (two levels up from API)
DotEnv.Load(options: new DotEnvOptions(
    envFilePaths: new[] { Path.Combine("..", "..", ".env") }));

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

var apiKey = builder.Configuration["OPENAI_API_KEY"]
    ?? throw new InvalidOperationException("OPENAI_API_KEY not set");

builder.Services.AddMemoryCache();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Use Fake in Development (free), Real in Production
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddScoped<IWhisperService, FakeWhisperService>();
    builder.Services.AddScoped<ITranslationService, FakeTranslationService>();
}
else
{
    builder.Services.AddHttpClient<IWhisperService, WhisperService>(c =>
    {
        c.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
        c.Timeout = TimeSpan.FromSeconds(30);
    });

    builder.Services.AddHttpClient<ITranslationService, TranslationService>(c =>
    {
        c.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
    });
}

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

// Phase 1 test endpoint — remove in Phase 2
app.MapPost("/api/transcribe", async (
    IFormFile audio,
    IWhisperService whisper,
    ITranslationService translator) =>
{
    using var ms = new MemoryStream();
    await audio.CopyToAsync(ms);

    var maltese = await whisper.TranscribeAsync(ms.ToArray());
    var english = await translator.TranslateAsync(maltese, "test-session");

    return Results.Ok(new { maltese, english });
}).DisableAntiforgery();

app.MapGet("/health", () => "OK");

app.Run();
