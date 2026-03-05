using FluentAssertions;
using MalteseTranscriber.Infrastructure;

namespace MalteseTranscriber.Tests.Infrastructure;

public class FakeTranslationServiceTests
{
    [Theory]
    [InlineData("Bongu, kif int?", "Good morning, how are you?")]
    [InlineData("Grazzi ħafna għal kollox.", "Thank you very much for everything.")]
    [InlineData("Il-maltese huwa lingwa unika.", "Maltese is a unique language.")]
    public async Task TranslateAsync_Should_ReturnKnownTranslation_When_GivenKnownPhrase(
        string maltese, string expectedEnglish)
    {
        // Arrange
        var sut = new FakeTranslationService();

        // Act
        var result = await sut.TranslateAsync(maltese, "session-1");

        // Assert
        result.Should().Be(expectedEnglish);
    }

    [Fact]
    public async Task TranslateAsync_Should_ReturnFallback_When_GivenUnknownPhrase()
    {
        // Arrange
        var sut = new FakeTranslationService();

        // Act
        var result = await sut.TranslateAsync("Unknown phrase", "session-1");

        // Assert
        result.Should().Contain("Unknown phrase");
        result.Should().StartWith("[Translation of:");
    }

    [Fact]
    public async Task TranslateAsync_Should_IgnoreSessionId_When_Translating()
    {
        // Arrange
        var sut = new FakeTranslationService();

        // Act
        var result1 = await sut.TranslateAsync("Bongu, kif int?", "session-a");
        var result2 = await sut.TranslateAsync("Bongu, kif int?", "session-b");

        // Assert
        result1.Should().Be(result2);
    }
}
