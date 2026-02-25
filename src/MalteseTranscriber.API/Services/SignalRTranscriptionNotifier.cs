using MalteseTranscriber.API.Hubs;
using MalteseTranscriber.Core.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace MalteseTranscriber.API.Services;

public class SignalRTranscriptionNotifier : ITranscriptionNotifier
{
    private readonly IHubContext<TranscriptionHub> _hub;

    public SignalRTranscriptionNotifier(IHubContext<TranscriptionHub> hub) => _hub = hub;

    public Task SendMalteseTranscriptionAsync(string sessionId, int chunkIndex, string text) =>
        _hub.Clients.Group(sessionId).SendAsync("OnMalteseTranscription", new
        {
            ChunkIndex = chunkIndex,
            Text = text,
            Timestamp = DateTimeOffset.UtcNow
        });

    public Task SendEnglishTranslationAsync(string sessionId, int chunkIndex, string originalText, string translatedText) =>
        _hub.Clients.Group(sessionId).SendAsync("OnEnglishTranslation", new
        {
            ChunkIndex = chunkIndex,
            OriginalText = originalText,
            TranslatedText = translatedText,
            Timestamp = DateTimeOffset.UtcNow
        });

    public Task SendErrorAsync(string sessionId, int chunkIndex, string message) =>
        _hub.Clients.Group(sessionId).SendAsync("OnError", new
        {
            ChunkIndex = chunkIndex,
            Message = message
        });
}
