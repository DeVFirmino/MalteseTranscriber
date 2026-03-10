using FluentAssertions;
using FluentValidation.TestHelper;
using MalteseTranscriber.Core.Requests;
using MalteseTranscriber.Core.Validators;

namespace MalteseTranscriber.Tests.Validators;

public class StartSessionRequestValidatorTests
{
    private readonly StartSessionRequestValidator _validator = new();

    [Fact]
    public void Validate_Should_Pass_When_SessionIdIsValid()
    {
        // Arrange
        var request = new StartSessionRequest("my-session-123");

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_Should_Pass_When_SessionIdContainsDashes()
    {
        // Arrange
        var request = new StartSessionRequest("abc-def-123-ghi");

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_Should_Fail_When_SessionIdIsEmpty()
    {
        // Arrange
        var request = new StartSessionRequest("");

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SessionId)
            .WithErrorMessage("SessionId is required.");
    }

    [Fact]
    public void Validate_Should_Fail_When_SessionIdIsNull()
    {
        // Arrange
        var request = new StartSessionRequest(null!);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SessionId);
    }

    [Fact]
    public void Validate_Should_Fail_When_SessionIdExceedsMaxLength()
    {
        // Arrange
        var request = new StartSessionRequest(new string('a', 129));

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SessionId)
            .WithErrorMessage("SessionId exceeds maximum length.");
    }

    [Fact]
    public void Validate_Should_Pass_When_SessionIdIsExactlyMaxLength()
    {
        // Arrange
        var request = new StartSessionRequest(new string('a', 128));

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("session@!#")]
    [InlineData("session with spaces")]
    [InlineData("session.with.dots")]
    [InlineData("session_with_underscores")]
    public void Validate_Should_Fail_When_SessionIdContainsSpecialCharacters(string sessionId)
    {
        // Arrange
        var request = new StartSessionRequest(sessionId);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SessionId)
            .WithErrorMessage("SessionId must be alphanumeric with dashes only.");
    }
}
