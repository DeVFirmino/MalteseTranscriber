using FluentAssertions;
using MalteseTranscriber.Infrastructure;

namespace MalteseTranscriber.Tests.Infrastructure;

public class FakeWhisperServiceTests
{
    [Fact]
    public async Task TranscribeAsync_Should_ReturnMalteseText_When_Called()
    {
        // Arrange
        var sut = new FakeWhisperService();

        // Act
        var result = await sut.TranscribeAsync(new byte[100]);

        // Assert
        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task TranscribeAsync_Should_CycleThroughPhrases_When_CalledMultipleTimes()
    {
        // Arrange
        var sut = new FakeWhisperService();
        var results = new List<string>();

        // Act
        for (var i = 0; i < 6; i++)
            results.Add(await sut.TranscribeAsync(new byte[100]));

        // Assert — should cycle (6th == 1st)
        results[5].Should().Be(results[0]);
        results.Take(5).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task TranscribeAsync_Should_ReturnKnownMaltesePhrase_When_CalledFirst()
    {
        // Arrange
        var sut = new FakeWhisperService();

        // Act
        var result = await sut.TranscribeAsync(new byte[100]);

        // Assert
        result.Should().Be("Bongu, kif int?");
    }

    [Fact]
    public async Task TranscribeAsync_Should_IgnoreInputBytes_When_AnyDataProvided()
    {
        // Arrange
        var sut = new FakeWhisperService();

        // Act
        var result1 = await sut.TranscribeAsync(new byte[10]);
        var sut2 = new FakeWhisperService();
        var result2 = await sut2.TranscribeAsync(new byte[10000]);

        // Assert — same index produces same phrase regardless of input
        result1.Should().Be(result2);
    }
}
