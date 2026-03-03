using FluentAssertions;
using MalteseTranscriber.API.Hubs;
using MalteseTranscriber.API.Services;
using Microsoft.AspNetCore.SignalR;
using NSubstitute;

namespace MalteseTranscriber.Tests.API;

public class SignalRTranscriptionNotifierTests
{
    private readonly IHubContext<TranscriptionHub> _hubContext;
    private readonly IClientProxy _clientProxy;
    private readonly SignalRTranscriptionNotifier _notifier;

    public SignalRTranscriptionNotifierTests()
    {
        _hubContext = Substitute.For<IHubContext<TranscriptionHub>>();
        _clientProxy = Substitute.For<IClientProxy>();

        var hubClients = Substitute.For<IHubClients>();
        hubClients.Group(Arg.Any<string>()).Returns(_clientProxy);
        _hubContext.Clients.Returns(hubClients);

        _notifier = new SignalRTranscriptionNotifier(_hubContext);
    }

    [Fact]
    public async Task SendMalteseTranscriptionAsync_Should_SendToGroup_When_Called()
    {
        // Arrange & Act
        await _notifier.SendMalteseTranscriptionAsync("session-1", 0, "Bonġu");

        // Assert
        _hubContext.Clients.Received(1).Group("session-1");
        await _clientProxy.Received(1).SendCoreAsync(
            "OnMalteseTranscription",
            Arg.Is<object?[]>(args => args.Length == 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendEnglishTranslationAsync_Should_SendToGroup_When_Called()
    {
        // Arrange & Act
        await _notifier.SendEnglishTranslationAsync("session-1", 0, "Bonġu", "Hello");

        // Assert
        _hubContext.Clients.Received(1).Group("session-1");
        await _clientProxy.Received(1).SendCoreAsync(
            "OnEnglishTranslation",
            Arg.Is<object?[]>(args => args.Length == 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendErrorAsync_Should_SendToGroup_When_Called()
    {
        // Arrange & Act
        await _notifier.SendErrorAsync("session-1", 5, "Something went wrong");

        // Assert
        _hubContext.Clients.Received(1).Group("session-1");
        await _clientProxy.Received(1).SendCoreAsync(
            "OnError",
            Arg.Is<object?[]>(args => args.Length == 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendMalteseTranscriptionAsync_Should_TargetCorrectGroup_When_DifferentSessions()
    {
        // Arrange & Act
        await _notifier.SendMalteseTranscriptionAsync("session-A", 0, "Bonġu");
        await _notifier.SendMalteseTranscriptionAsync("session-B", 1, "Saħħa");

        // Assert
        _hubContext.Clients.Received(1).Group("session-A");
        _hubContext.Clients.Received(1).Group("session-B");
    }
}
