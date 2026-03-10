using FluentAssertions;
using MalteseTranscriber.Core.Interfaces;
using MalteseTranscriber.Core.Models;
using MalteseTranscriber.Core.Options;
using MalteseTranscriber.Infrastructure.Pipeline;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace MalteseTranscriber.Tests.Infrastructure;

public class TranscriptionPipelineTests
{
    private readonly IStreamingTranscriptionService _transcription = Substitute.For<IStreamingTranscriptionService>();
    private readonly ITranslationService _translator = Substitute.For<ITranslationService>();
    private readonly ITranscriptionNotifier _notifier = Substitute.For<ITranscriptionNotifier>();
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
    private readonly ILogger<TranscriptionPipeline> _logger = Substitute.For<ILogger<TranscriptionPipeline>>();
    private readonly TranscriptionPipeline _pipeline;

    public TranscriptionPipelineTests()
    {
        var sessionOptions = Options.Create(new SessionOptions());
        _pipeline = new TranscriptionPipeline(_transcription, _translator, _notifier, _cache, _logger, sessionOptions);
    }

    [Fact]
    public async Task InitializeSessionAsync_Should_CacheSession_When_Called()
    {
        // Arrange
        var sessionId = "test-session";

        // Act
        await _pipeline.InitializeSessionAsync(sessionId, "conn-1");

        // Assert
        var session = _cache.Get<TranscriptionSession>($"session:{sessionId}");
        session.Should().NotBeNull();
        session!.SessionId.Should().Be(sessionId);
        session.ConnectionId.Should().Be("conn-1");
    }

    [Fact]
    public async Task InitializeSessionAsync_Should_ConnectTranscription_When_Called()
    {
        // Arrange & Act
        await _pipeline.InitializeSessionAsync("test-session", "conn-1");

        // Assert — streaming service should be connected with a callback
        await _transcription.Received(1).ConnectAsync(
            "test-session", Arg.Any<Func<string, string, Task>>());
    }

    [Fact]
    public async Task ProcessChunkAsync_Should_ForwardAudioToService_When_SessionExists()
    {
        // Arrange
        await _pipeline.InitializeSessionAsync("test-session", "conn-1");
        var audioChunk = new byte[48_000];

        // Act
        await _pipeline.ProcessChunkAsync("test-session", audioChunk, 0);

        // Assert — audio should be forwarded directly, no buffering
        await _transcription.Received(1).SendAudioAsync("test-session", audioChunk);
    }

    [Fact]
    public async Task ProcessChunkAsync_Should_ReturnSilently_When_SessionNotFound()
    {
        // Arrange — no session initialized
        var chunk = new byte[48_000];

        // Act
        await _pipeline.ProcessChunkAsync("nonexistent", chunk, 0);

        // Assert — nothing should be forwarded
        await _transcription.DidNotReceive().SendAudioAsync(
            Arg.Any<string>(), Arg.Any<byte[]>());
    }

    [Fact]
    public async Task FinalizeSessionAsync_Should_DisconnectAndRemoveSession_When_Called()
    {
        // Arrange
        await _pipeline.InitializeSessionAsync("test-session", "conn-1");

        // Act
        await _pipeline.FinalizeSessionAsync("test-session");

        // Assert
        await _transcription.Received(1).DisconnectAsync("test-session");
        var session = _cache.Get<TranscriptionSession>("session:test-session");
        session.Should().BeNull();
    }

    [Fact]
    public async Task FinalizeSessionAsync_Should_NotThrow_When_SessionAlreadyFinalized()
    {
        // Arrange — no session

        // Act
        var act = () => _pipeline.FinalizeSessionAsync("nonexistent");

        // Assert
        await act.Should().NotThrowAsync();
    }
}
