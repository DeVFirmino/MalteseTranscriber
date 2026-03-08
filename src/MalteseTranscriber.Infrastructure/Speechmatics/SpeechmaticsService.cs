using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using MalteseTranscriber.Core.Interfaces;
using MalteseTranscriber.Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MalteseTranscriber.Infrastructure.Speechmatics;

/// <summary>
/// Real-time Speechmatics transcription via persistent WebSocket connections.
/// Manages one ClientWebSocket per session. Registered as Singleton.
/// </summary>
public class SpeechmaticsService : IStreamingTranscriptionService, IDisposable
{
    private readonly IConfiguration _config;
    private readonly ILogger<SpeechmaticsService> _logger;
    private readonly SpeechmaticsMessageHandler _messageHandler;
    private readonly ConcurrentDictionary<string, SpeechmaticsConnection> _connections = new();

    private const string Endpoint = "wss://eu.rt.speechmatics.com/v2";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public SpeechmaticsService(IConfiguration config, ILogger<SpeechmaticsService> logger)
    {
        _config = config;
        _logger = logger;
        _messageHandler = new SpeechmaticsMessageHandler(logger, JsonOptions);
    }

    public async Task ConnectAsync(string sessionId, Func<string, string, Task> onTranscript)
    {
        var apiKey = _config["SPEECHMATICS_API_KEY"]
            ?? throw new InvalidOperationException("SPEECHMATICS_API_KEY not configured");

        var ws = new ClientWebSocket();
        ws.Options.SetRequestHeader("Authorization", $"Bearer {apiKey}");

        _logger.LogInformation("Connecting to Speechmatics for session {SessionId}", sessionId);
        await ws.ConnectAsync(new Uri(Endpoint), CancellationToken.None);

        // Send StartRecognition with Maltese config
        await SendJsonAsync(ws, new StartRecognitionMessage());

        // Wait for RecognitionStarted confirmation
        var response = await ReceiveJsonAsync<SpeechmaticsResponse>(ws);
        if (response?.Message != "RecognitionStarted")
        {
            ws.Dispose();
            throw new InvalidOperationException(
                $"Speechmatics: expected RecognitionStarted, got '{response?.Message}'");
        }

        var cts = new CancellationTokenSource();
        var connection = new SpeechmaticsConnection(ws, onTranscript, cts);
        _connections[sessionId] = connection;

        // Start background receive loop for incoming transcripts
        _ = Task.Run(() => ReceiveLoopAsync(sessionId, connection));

        _logger.LogInformation("Speechmatics connected for session {SessionId}", sessionId);
    }

    public async Task SendAudioAsync(string sessionId, byte[] pcmBytes)
    {
        if (!_connections.TryGetValue(sessionId, out var conn))
        {
            _logger.LogWarning("SendAudioAsync: no connection for session {SessionId}", sessionId);
            return;
        }

        if (conn.WebSocket.State != WebSocketState.Open)
        {
            _logger.LogWarning("SendAudioAsync: WebSocket not open for session {SessionId}", sessionId);
            return;
        }

        // SemaphoreSlim ensures thread-safe sends on the same socket
        await conn.SendSemaphore.WaitAsync();
        try
        {
            await conn.WebSocket.SendAsync(
                pcmBytes, WebSocketMessageType.Binary, true, CancellationToken.None);
            conn.SequenceNumber++;
        }
        finally
        {
            conn.SendSemaphore.Release();
        }
    }

    public async Task DisconnectAsync(string sessionId)
    {
        if (!_connections.TryRemove(sessionId, out var conn))
            return;

        _logger.LogInformation("Disconnecting Speechmatics for session {SessionId}", sessionId);

        try
        {
            if (conn.WebSocket.State == WebSocketState.Open)
            {
                var endMsg = new EndOfStreamMessage { LastSeqNo = conn.SequenceNumber };
                await SendJsonAsync(conn.WebSocket, endMsg);

                // Allow time for final transcripts before closing
                await Task.Delay(2000);
            }

            await conn.CancellationSource.CancelAsync();

            if (conn.WebSocket.State == WebSocketState.Open)
            {
                await conn.WebSocket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure, "Session ended", CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during Speechmatics disconnect for {SessionId}", sessionId);
        }
        finally
        {
            conn.Dispose();
            _logger.LogInformation("Speechmatics disconnected for session {SessionId}", sessionId);
        }
    }

    /// <summary>
    /// Background loop that reads messages from Speechmatics and delegates
    /// handling to SpeechmaticsMessageHandler.
    /// </summary>
    private async Task ReceiveLoopAsync(string sessionId, SpeechmaticsConnection conn)
    {
        var buffer = new byte[8192];
        var messageBuffer = new List<byte>();

        try
        {
            while (!conn.CancellationSource.IsCancellationRequested
                   && conn.WebSocket.State == WebSocketState.Open)
            {
                var result = await conn.WebSocket.ReceiveAsync(
                    buffer, conn.CancellationSource.Token);

                if (result.MessageType == WebSocketMessageType.Close)
                    break;

                messageBuffer.AddRange(buffer.AsSpan(0, result.Count).ToArray());

                if (!result.EndOfMessage)
                    continue;

                var json = Encoding.UTF8.GetString(messageBuffer.ToArray());
                messageBuffer.Clear();

                await _messageHandler.HandleAsync(sessionId, json, conn);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }
        catch (WebSocketException ex) when (ex.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely)
        {
            _logger.LogWarning("Speechmatics connection closed prematurely for {SessionId}", sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Speechmatics receive loop error for {SessionId}", sessionId);
        }
    }

    private static async Task SendJsonAsync<T>(ClientWebSocket ws, T message)
    {
        var json = JsonSerializer.Serialize(message, JsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);
        await ws.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
    }

    private static async Task<T?> ReceiveJsonAsync<T>(ClientWebSocket ws)
    {
        var buffer = new byte[4096];
        var messageBuffer = new List<byte>();

        WebSocketReceiveResult result;
        do
        {
            result = await ws.ReceiveAsync(buffer, CancellationToken.None);
            messageBuffer.AddRange(buffer.AsSpan(0, result.Count).ToArray());
        } while (!result.EndOfMessage);

        var json = Encoding.UTF8.GetString(messageBuffer.ToArray());
        return JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    public void Dispose()
    {
        foreach (var (_, conn) in _connections)
        {
            conn.CancellationSource.Cancel();
            conn.Dispose();
        }
        _connections.Clear();
    }
}
