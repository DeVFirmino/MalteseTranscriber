using FluentAssertions;
using FluentValidation.TestHelper;
using MalteseTranscriber.Core.Requests;
using MalteseTranscriber.Core.Validators;

namespace MalteseTranscriber.Tests.Validators;

public class AudioChunkRequestValidatorTests
{
    private readonly AudioChunkRequestValidator _validator = new();

    private static string ValidBase64 => Convert.ToBase64String(new byte[100]);

    [Fact]
    public void Validate_Should_Pass_When_AllFieldsAreValid()
    {
        // Arrange
        var request = new AudioChunkRequest("session-1", ValidBase64, 0);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_Should_Fail_When_SessionIdIsEmpty()
    {
        // Arrange
        var request = new AudioChunkRequest("", ValidBase64, 0);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SessionId)
            .WithErrorMessage("SessionId is required.");
    }

    [Fact]
    public void Validate_Should_Fail_When_SessionIdExceedsMaxLength()
    {
        // Arrange
        var request = new AudioChunkRequest(new string('a', 129), ValidBase64, 0);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SessionId)
            .WithErrorMessage("SessionId exceeds maximum length.");
    }

    [Fact]
    public void Validate_Should_Fail_When_AudioBase64IsEmpty()
    {
        // Arrange
        var request = new AudioChunkRequest("session-1", "", 0);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AudioBase64)
            .WithErrorMessage("AudioBase64 is required.");
    }

    [Fact]
    public void Validate_Should_Fail_When_AudioBase64IsInvalid()
    {
        // Arrange
        var request = new AudioChunkRequest("session-1", "not-valid-base64!!!", 0);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AudioBase64)
            .WithErrorMessage("AudioBase64 is not valid base64.");
    }

    [Fact]
    public void Validate_Should_Fail_When_AudioBase64ExceedsMaxSize()
    {
        // Arrange — 2MB+ of base64
        var largeData = Convert.ToBase64String(new byte[1_600_000]);
        var request = new AudioChunkRequest("session-1", largeData, 0);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AudioBase64)
            .WithErrorMessage("Audio chunk exceeds maximum size of 1.5MB.");
    }

    [Fact]
    public void Validate_Should_Fail_When_ChunkIndexIsNegative()
    {
        // Arrange
        var request = new AudioChunkRequest("session-1", ValidBase64, -1);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ChunkIndex)
            .WithErrorMessage("ChunkIndex must be non-negative.");
    }

    [Fact]
    public void Validate_Should_Pass_When_ChunkIndexIsZero()
    {
        // Arrange
        var request = new AudioChunkRequest("session-1", ValidBase64, 0);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ChunkIndex);
    }

    [Fact]
    public void Validate_Should_Pass_When_ChunkIndexIsLargePositive()
    {
        // Arrange
        var request = new AudioChunkRequest("session-1", ValidBase64, 9999);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ChunkIndex);
    }
}
