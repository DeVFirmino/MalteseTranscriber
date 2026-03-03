using FluentAssertions;
using MalteseTranscriber.Core.Interfaces;
using MalteseTranscriber.Core.Models;
using MalteseTranscriber.Infrastructure;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace MalteseTranscriber.Tests.Infrastructure;

public class TranscriptionPipelineTests
{
    private readonly IWhisperService _whisper = Substitute.For<IWhisperService>();
    private readonly ITranslationService _translator = Substitute.For<ITranslationService>();
    private readonly ITranscriptionNotifier _notifier = Substitute.For<ITranscriptionNotifier>();
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
    private readonly ILogger<TranscriptionPipeline> _logger = Substitute.For<ILogger<TranscriptionPipeline>>();
    private readonly TranscriptionPipeline _pipeline;

    public TranscriptionPipelineTests()
    {
        _pipeline = new TranscriptionPipeline(_whisper, _translator, _notifier, _cache, _logger);
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
    public async Task InitializeSessionAsync_Should_CreateEmptyBuffers_When_NewSession()
    {
        // Arrange & Act
        await _pipeline.InitializeSessionAsync("test-session", "conn-1");

        // Assert
        var session = _cache.Get<TranscriptionSession>("session:test-session");
        session!.AudioBuffer.Should().BeEmpty();
        session.OverlapBuffer.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessChunkAsync_Should_BufferAudio_When_BelowMinimumThreshold()
    {
        // Arrange
        await _pipeline.InitializeSessionAsync("test-session", "conn-1");
        var smallChunk = new byte[1000]; // well below 96,000 byte threshold

        // Act
        await _pipeline.ProcessChunkAsync("test-session", smallChunk, 0);

        // Assert — audio should be buffered but no transcription triggered
        await _whisper.DidNotReceive().TranscribeAsync(Arg.Any<byte[]>(), Arg.Any<string>());
    }

    [Fact]
    public async Task ProcessChunkAsync_Should_TriggerTranscription_When_BufferReachesThreshold()
    {
        // Arrange
        await _pipeline.InitializeSessionAsync("test-session", "conn-1");
        var largeChunk = new byte[96_000]; // exactly the threshold
        _whisper.TranscribeAsync(Arg.Any<byte[]>(), Arg.Any<string>())
            .Returns("Bonġu");
        _translator.TranslateAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns("Hello");

        // Act
        await _pipeline.ProcessChunkAsync("test-session", largeChunk, 0);

        // Allow background Task.Run to complete
        await Task.Delay(500);

        // Assert
        await _whisper.Received(1).TranscribeAsync(Arg.Any<byte[]>(), "mt");
    }

    [Fact]
    public async Task ProcessChunkAsync_Should_ReturnSilently_When_SessionNotFound()
    {
        // Arrange — no session initialized
        var chunk = new byte[100_000];

        // Act
        await _pipeline.ProcessChunkAsync("nonexistent", chunk, 0);

        // Assert — nothing should happen
        await _whisper.DidNotReceive().TranscribeAsync(Arg.Any<byte[]>(), Arg.Any<string>());
        await _notifier.DidNotReceive().SendErrorAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string>());
    }

    [Fact]
    public async Task ProcessChunkAsync_Should_SendMalteseAndEnglish_When_TranscriptionSucceeds()
    {
        // Arrange
        await _pipeline.InitializeSessionAsync("test-session", "conn-1");
        var largeChunk = new byte[96_000];
        _whisper.TranscribeAsync(Arg.Any<byte[]>(), Arg.Any<string>())
            .Returns("Bonġu");
        _translator.TranslateAsync("Bonġu", "test-session")
            .Returns("Hello");

        // Act
        await _pipeline.ProcessChunkAsync("test-session", largeChunk, 0);
        await Task.Delay(500);

        // Assert
        await _notifier.Received(1).SendMalteseTranscriptionAsync("test-session", 0, "Bonġu");
        await _notifier.Received(1).SendEnglishTranslationAsync("test-session", 0, "Bonġu", "Hello");
    }

    [Fact]
    public async Task ProcessChunkAsync_Should_SendError_When_WhisperThrows()
    {
        // Arrange
        await _pipeline.InitializeSessionAsync("test-session", "conn-1");
        var largeChunk = new byte[96_000];
        _whisper.TranscribeAsync(Arg.Any<byte[]>(), Arg.Any<string>())
            .Returns<string>(x => throw new HttpRequestException("API timeout"));

        // Act
        await _pipeline.ProcessChunkAsync("test-session", largeChunk, 0);
        await Task.Delay(500);

        // Assert
        await _notifier.Received(1).SendErrorAsync("test-session", 0, "API timeout");
    }

    [Fact]
    public async Task ProcessChunkAsync_Should_SkipTranslation_When_WhisperReturnsEmpty()
    {
        // Arrange
        await _pipeline.InitializeSessionAsync("test-session", "conn-1");
        var largeChunk = new byte[96_000];
        _whisper.TranscribeAsync(Arg.Any<byte[]>(), Arg.Any<string>())
            .Returns("");

        // Act
        await _pipeline.ProcessChunkAsync("test-session", largeChunk, 0);
        await Task.Delay(500);

        // Assert — translation should not be called
        await _translator.DidNotReceive().TranslateAsync(Arg.Any<string>(), Arg.Any<string>());
        await _notifier.DidNotReceive().SendMalteseTranscriptionAsync(
            Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string>());
    }

    [Fact]
    public async Task FinalizeSessionAsync_Should_RemoveSessionFromCache_When_Called()
    {
        // Arrange
        await _pipeline.InitializeSessionAsync("test-session", "conn-1");

        // Act
        await _pipeline.FinalizeSessionAsync("test-session");

        // Assert
        var session = _cache.Get<TranscriptionSession>("session:test-session");
        session.Should().BeNull();
    }

    [Fact]
    public async Task ProcessChunkAsync_Should_MaintainOverlapBuffer_When_Processing()
    {
        // Arrange
        await _pipeline.InitializeSessionAsync("test-session", "conn-1");
        // Fill with recognizable data
        var chunk = new byte[96_000];
        for (int i = 0; i < chunk.Length; i++)
            chunk[i] = (byte)(i % 256);

        _whisper.TranscribeAsync(Arg.Any<byte[]>(), Arg.Any<string>())
            .Returns("Bonġu");
        _translator.TranslateAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns("Hello");

        // Act
        await _pipeline.ProcessChunkAsync("test-session", chunk, 0);
        await Task.Delay(500);

        // Assert — overlap buffer should be 16,000 bytes (last 16KB of processed audio)
        var session = _cache.Get<TranscriptionSession>("session:test-session");
        session!.OverlapBuffer.Length.Should().Be(16_000);
    }
}
