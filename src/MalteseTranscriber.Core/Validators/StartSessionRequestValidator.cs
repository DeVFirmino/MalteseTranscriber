using FluentValidation;
using MalteseTranscriber.Core.Requests;

namespace MalteseTranscriber.Core.Validators;

public class StartSessionRequestValidator : AbstractValidator<StartSessionRequest>
{
    public StartSessionRequestValidator()
    {
        RuleFor(x => x.SessionId)
            .NotEmpty().WithMessage("SessionId is required.")
            .MaximumLength(128).WithMessage("SessionId exceeds maximum length.")
            .Matches(@"^[a-zA-Z0-9\-]+$").WithMessage("SessionId must be alphanumeric with dashes only.");
    }
}
