using FluentValidation;
using MalteseTranscriber.Core.Options;

namespace MalteseTranscriber.API.Extensions;

public static class ApplicationExtensions
{
    /// <summary>
    /// Registers application-level services: caching, validation, CORS, and API exploration.
    /// </summary>
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<MalteseTranscriber.Core.Options.SessionOptions>(
            configuration.GetSection("Session"));

        services.AddMemoryCache();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        services.AddCors(opts => opts.AddPolicy("Frontend", p =>
            p.WithOrigins(
                configuration["FRONTEND_URL"] ?? "http://localhost:3000",
                "http://localhost:5173",
                "http://localhost:8080",
                "http://localhost:5001")
             .AllowAnyHeader()
             .AllowAnyMethod()
             .AllowCredentials()));

        services.AddValidatorsFromAssemblyContaining<
            MalteseTranscriber.Core.Validators.AudioChunkRequestValidator>();

        return services;
    }
}
