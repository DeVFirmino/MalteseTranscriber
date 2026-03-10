using dotenv.net;
using MalteseTranscriber.API.Extensions;
using MalteseTranscriber.API.Hubs;
using MalteseTranscriber.API.Middleware;
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

    builder.Services.AddApplicationServices(builder.Configuration);
    builder.Services.AddInfrastructureServices(builder.Configuration, builder.Environment);
    builder.Services.AddAppRateLimiting();

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
