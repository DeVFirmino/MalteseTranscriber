using dotenv.net;
using MalteseTranscriber.API.Hubs;
using MalteseTranscriber.API.Services;
using MalteseTranscriber.Core.Interfaces;
using MalteseTranscriber.Infrastructure;

DotEnv.Load(options: new DotEnvOptions(
    envFilePaths: new[] { Path.Combine("..", "..", ".env") }));

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

var apiKey = builder.Configuration["OPENAI_API_KEY"]
    ?? throw new InvalidOperationException("OPENAI_API_KEY not set");

// SignalR
builder.Services.AddSignalR(opts =>
{
    opts.MaximumReceiveMessageSize = 1_048_576;
    opts.EnableDetailedErrors = builder.Environment.IsDevelopment();
});

builder.Services.AddMemoryCache();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS — AllowCredentials is required for SignalR
builder.Services.AddCors(opts => opts.AddPolicy("Frontend", p =>
    p.WithOrigins(
        builder.Configuration["FRONTEND_URL"] ?? "http://localhost:3000",
        "http://localhost:5173",
        "http://localhost:5000")
     .AllowAnyHeader()
     .AllowAnyMethod()
     .AllowCredentials()));

// Whisper + Translation: fake in dev, real in prod
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

// Pipeline + Notifier
builder.Services.AddScoped<ITranscriptionPipeline, TranscriptionPipeline>();
builder.Services.AddSingleton<ITranscriptionNotifier, SignalRTranscriptionNotifier>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseCors("Frontend");

app.MapHub<TranscriptionHub>("/hubs/transcription");
app.MapGet("/health", () => "OK");

app.Run();
