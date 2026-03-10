using System.Text.Json;
using MalteseTranscriber.Infrastructure.Speechmatics.Models;
using Microsoft.Extensions.Logging;

namespace MalteseTranscriber.Infrastructure.Speechmatics;

/// <summary>
/// Parses incoming Speechmatics WebSocket messages and dispatches
/// finalized transcripts to the connection's callback.
/// </summary>
internal class SpeechmaticsMessageHandler
{
    private readonly ILogger _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public SpeechmaticsMessageHandler(ILogger logger, JsonSerializerOptions jsonOptions)
    {
        _logger = logger;
        _jsonOptions = jsonOptions;
    }

    /// <summary>
    /// Handles a single JSON message received from the Speechmatics WebSocket.
    /// Fires the transcript callback for AddTranscript messages with non-empty text.
    /// </summary>
    public async Task HandleAsync(string sessionId, string json, SpeechmaticsConnection conn)
    {
        var baseMsg = JsonSerializer.Deserialize<SpeechmaticsResponse>(json, _jsonOptions);

        switch (baseMsg?.Message)
        {
            case "AddTranscript":
                await HandleTranscriptAsync(sessionId, json, conn);
                break;

            case "AddPartialTranscript":
                // Partial results ignored — only act on finalized transcripts
                break;

            case "AudioAdded":
            case "RecognitionStarted":
                break;

            case "EndOfTranscript":
                _logger.LogInformation("Speechmatics EndOfTranscript for {SessionId}", sessionId);
                break;

            case "Warning":
                _logger.LogWarning("Speechmatics warning [{SessionId}]: {Json}", sessionId, json);
                break;

            case "Error":
                HandleError(sessionId, json);
                break;

            default:
                _logger.LogDebug("Speechmatics [{SessionId}]: {MessageType}", sessionId, baseMsg?.Message);
                break;
        }
    }

    private async Task HandleTranscriptAsync(string sessionId, string json, SpeechmaticsConnection conn)
    {
        var transcript = JsonSerializer.Deserialize<AddTranscriptResponse>(json, _jsonOptions);
        var text = transcript?.Metadata?.Transcript?.Trim();

        if (string.IsNullOrWhiteSpace(text))
            return;

        _logger.LogInformation("[{SessionId}] Transcript: {Text}", sessionId, text);
        await conn.OnTranscript(sessionId, text);
    }

    private void HandleError(string sessionId, string json)
    {
        var error = JsonSerializer.Deserialize<SpeechmaticsErrorResponse>(json, _jsonOptions);
        _logger.LogError("Speechmatics error [{SessionId}]: {Type} — {Reason}",
            sessionId, error?.Type, error?.Reason);
    }
}
