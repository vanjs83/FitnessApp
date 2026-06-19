using FitnessApp.Application.DTOs.Appointments;
using FluentValidation;

namespace FitnessApp.Application.Validators.Appointments;

public class CreateAppointmentRequestValidator : AbstractValidator<CreateAppointmentRequest>
{
    public CreateAppointmentRequestValidator()
    {
        RuleFor(x => x.ClientId).NotEmpty();
        RuleFor(x => x.StartsAt).NotEmpty();
        RuleFor(x => x.DurationMinutes).InclusiveBetween(5, 480);
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Location).MaximumLength(400);
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

public class RequestAppointmentRequestValidator : AbstractValidator<RequestAppointmentRequest>
{
    public RequestAppointmentRequestValidator()
    {
        RuleFor(x => x.StartsAt).NotEmpty();
        RuleFor(x => x.DurationMinutes).InclusiveBetween(5, 480);
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Location).MaximumLength(400);
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

public class UpdateAppointmentRequestValidator : AbstractValidator<UpdateAppointmentRequest>
{
    public UpdateAppointmentRequestValidator()
    {
        RuleFor(x => x.StartsAt).NotEmpty();
        RuleFor(x => x.DurationMinutes).InclusiveBetween(5, 480);
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Location).MaximumLength(400);
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}
