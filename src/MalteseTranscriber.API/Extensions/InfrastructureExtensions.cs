using MalteseTranscriber.API.Services;
using MalteseTranscriber.Core.Interfaces;
using MalteseTranscriber.Core.Options;
using MalteseTranscriber.Infrastructure.Fakes;
using MalteseTranscriber.Infrastructure.Pipeline;
using MalteseTranscriber.Infrastructure.Speechmatics;
using MalteseTranscriber.Infrastructure.Translation;

namespace MalteseTranscriber.API.Extensions;

public static class InfrastructureExtensions
{
    /// <summary>
    /// Registers transcription and translation services.
    /// Development: uses Fakes (no API costs).
    /// Production: uses Speechmatics + OpenAI GPT-4o.
    /// </summary>
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        var maxMessageSize = configuration.GetValue<long>("SignalR:MaximumReceiveMessageSizeBytes", 1_048_576);
        services.AddSignalR(opts =>
        {
            opts.MaximumReceiveMessageSize = maxMessageSize;
            opts.EnableDetailedErrors = environment.IsDevelopment();
        });

        if (environment.IsDevelopment())
        {
            services.AddSingleton<IStreamingTranscriptionService, FakeStreamingTranscriptionService>();
            services.AddScoped<ITranslationService, FakeTranslationService>();
        }
        else
        {
            // Speechmatics real-time WebSocket for Maltese transcription
            services.Configure<SpeechmaticsOptions>(opts =>
            {
                configuration.GetSection("Speechmatics").Bind(opts);
                opts.ApiKey = configuration["SPEECHMATICS_API_KEY"]
                    ?? throw new InvalidOperationException("SPEECHMATICS_API_KEY not set");
            });
            services.AddSingleton<IStreamingTranscriptionService, SpeechmaticsService>();

            // OpenAI GPT-4o for Maltese → English translation
            services.Configure<TranslationOptions>(configuration.GetSection("Translation"));
            var openAiKey = configuration["OPENAI_API_KEY"]
                ?? throw new InvalidOperationException("OPENAI_API_KEY not set");
            services.AddHttpClient<ITranslationService, TranslationService>(c =>
            {
                c.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", openAiKey);
            });
        }

        services.AddScoped<ITranscriptionPipeline, TranscriptionPipeline>();
        services.AddSingleton<ITranscriptionNotifier, SignalRTranscriptionNotifier>();

        return services;
    }
}
