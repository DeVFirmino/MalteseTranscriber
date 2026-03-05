using FluentValidation;
using MalteseTranscriber.Core.Requests;

namespace MalteseTranscriber.Core.Validators;

public class AudioChunkRequestValidator : AbstractValidator<AudioChunkRequest>
{
    private const int MaxBase64Length = 2_097_152; // ~1.5MB decoded

    public AudioChunkRequestValidator()
    {
        RuleFor(x => x.SessionId)
            .NotEmpty().WithMessage("SessionId is required.")
            .MaximumLength(128).WithMessage("SessionId exceeds maximum length.");

        RuleFor(x => x.AudioBase64)
            .NotEmpty().WithMessage("AudioBase64 is required.")
            .MaximumLength(MaxBase64Length)
                .WithMessage("Audio chunk exceeds maximum size of 1.5MB.")
            .Must(BeValidBase64).WithMessage("AudioBase64 is not valid base64.");

        RuleFor(x => x.ChunkIndex)
            .GreaterThanOrEqualTo(0).WithMessage("ChunkIndex must be non-negative.");
    }

    private static bool BeValidBase64(string? value)
    {
        if (string.IsNullOrEmpty(value)) return false;
        try
        {
            Convert.FromBase64String(value);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
