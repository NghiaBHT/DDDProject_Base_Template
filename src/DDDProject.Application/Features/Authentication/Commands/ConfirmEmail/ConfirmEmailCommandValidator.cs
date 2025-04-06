using FluentValidation;

namespace DDDProject.Application.Features.Authentication.Commands.ConfirmEmail;

public class ConfirmEmailCommandValidator : AbstractValidator<ConfirmEmailCommand>
{
    public ConfirmEmailCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");
            // Optional: Add Guid validation if you parse it here, but likely better handled in handler
            //.Must(BeAValidGuid).WithMessage("User ID must be a valid GUID.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Confirmation code is required.");
    }

    // Example helper if validating GUID format here
    // private bool BeAValidGuid(string guidString)
    // {
    //     return Guid.TryParse(guidString, out _);
    // }
} 