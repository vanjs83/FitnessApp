using FitnessApp.Application.DTOs.Notifications;
using FluentValidation;

namespace FitnessApp.Application.Validators.Notifications;

public class RegisterDeviceRequestValidator : AbstractValidator<RegisterDeviceRequest>
{
    private static readonly string[] AllowedPlatforms = { "web", "android", "ios" };

    public RegisterDeviceRequestValidator()
    {
        RuleFor(x => x.Token).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Platform)
            .NotEmpty()
            .Must(p => AllowedPlatforms.Contains(p))
            .WithMessage("Platform must be 'web', 'android' or 'ios'.");
    }
}
