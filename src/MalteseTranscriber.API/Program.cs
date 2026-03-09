using dotenv.net;
using FluentValidation;
using MalteseTranscriber.API.Hubs;
using MalteseTranscriber.API.Middleware;
using MalteseTranscriber.API.Services;
using MalteseTranscriber.Core.Interfaces;
using MalteseTranscriber.Infrastructure;
using MalteseTranscriber.Infrastructure.Speechmatics;
using Serilog;

// Bootstrap logger for startup errors
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    DotEnv.Load(options: new DotEnvOptions(
        envFilePaths: new[] { Path.Combine("..", "..", ".env") }));

    var builder = WebApplication.CreateBuilder(args);
    builder.Configuration.AddEnvironmentVariables();

    // Serilog — structured logging
    builder.Host.UseSerilog((ctx, cfg) => cfg
        .ReadFrom.Configuration(ctx.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "MalteseTranscriber")
        .WriteTo.Console(outputTemplate:
            "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
        .WriteTo.File("logs/app-.log",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 7));

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

    // Rate limiting
    builder.Services.AddAppRateLimiting();

    // CORS — AllowCredentials is required for SignalR
    builder.Services.AddCors(opts => opts.AddPolicy("Frontend", p =>
        p.WithOrigins(
            builder.Configuration["FRONTEND_URL"] ?? "http://localhost:3000",
            "http://localhost:5173",
            "http://localhost:5001")
         .AllowAnyHeader()
         .AllowAnyMethod()
         .AllowCredentials()));

    // Transcription + Translation: fake in dev, real in prod
    if (builder.Environment.IsDevelopment())
    {
        builder.Services.AddSingleton<IStreamingTranscriptionService, FakeStreamingTranscriptionService>();
        builder.Services.AddScoped<ITranslationService, FakeTranslationService>();
    }
    else
    {
        // Speechmatics real-time WebSocket for Maltese transcription
        var speechmaticsKey = builder.Configuration["SPEECHMATICS_API_KEY"]
            ?? throw new InvalidOperationException("SPEECHMATICS_API_KEY not set");
        builder.Services.AddSingleton<IStreamingTranscriptionService, SpeechmaticsService>();

        // OpenAI GPT-4o for Maltese → English translation
        builder.Services.AddHttpClient<ITranslationService, TranslationService>(c =>
        {
            c.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
        });
    }

    // Pipeline + Notifier
    builder.Services.AddScoped<ITranscriptionPipeline, TranscriptionPipeline>();
    builder.Services.AddSingleton<ITranscriptionNotifier, SignalRTranscriptionNotifier>();

    // Validators
    builder.Services.AddValidatorsFromAssemblyContaining<MalteseTranscriber.Core.Validators.AudioChunkRequestValidator>();

    var app = builder.Build();

    // Middleware pipeline (order matters)
    app.UseMiddleware<GlobalExceptionMiddleware>();
    app.UseSerilogRequestLogging();
    app.UseRateLimiter();
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDefaultFiles();
    app.UseStaticFiles();
    app.UseCors("Frontend");

    app.MapHub<TranscriptionHub>("/hubs/transcription")
       .RequireRateLimiting("signalr");

    app.MapGet("/health", () => "OK");

    Log.Information("MalteseTranscriber starting up");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
