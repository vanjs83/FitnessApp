using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.DTOs.Email;
using FitnessApp.Application.Interfaces;
using FitnessApp.Domain.Common;
using MediatR;

namespace FitnessApp.Application.Features.Admin.Commands;

public record SendEmailToTrainersCommand(
    IReadOnlyList<string> TrainerIds,
    string Subject,
    string Body,
    string? Language) : IRequest<Result<EmailSendResultDto>>;

public class SendEmailToTrainersCommandHandler : IRequestHandler<SendEmailToTrainersCommand, Result<EmailSendResultDto>>
{
    private const string SenderName = "FitnessApp";

    private readonly IIdentityAdminService _identity;
    private readonly ICurrentUserService _currentUser;
    private readonly IEmailService _email;

    public SendEmailToTrainersCommandHandler(
        IIdentityAdminService identity, ICurrentUserService currentUser, IEmailService email)
    {
        _identity = identity;
        _currentUser = currentUser;
        _email = email;
    }

    public async Task<Result<EmailSendResultDto>> Handle(SendEmailToTrainersCommand request, CancellationToken cancellationToken)
    {
        var admin = await _identity.FindByIdAsync(_currentUser.UserId, cancellationToken);
        if (admin == null || string.IsNullOrWhiteSpace(admin.Email))
            return Result<EmailSendResultDto>.Fail(ResultError.Validation, "Administrator profil nema email.");

        var lang = (request.Language ?? "hr").ToLowerInvariant();
        var trainers = await _identity.GetUsersInRoleAsync(Roles.Trainer, cancellationToken);
        var trainersById = trainers.ToDictionary(t => t.Id);

        var result = new EmailSendResultDto();

        foreach (var trainerId in request.TrainerIds.Distinct())
        {
            if (!trainersById.TryGetValue(trainerId, out var trainer))
            {
                result.Failed.Add(new EmailFailureDto
                {
                    TrainerId = trainerId,
                    Email = string.Empty,
                    Error = "Trener nije pronađen ili nema rolu Trainer."
                });
                continue;
            }

            if (string.IsNullOrWhiteSpace(trainer.Email))
            {
                result.Failed.Add(new EmailFailureDto
                {
                    TrainerId = trainerId,
                    Email = string.Empty,
                    Error = "Trener nema email adresu."
                });
                continue;
            }

            var (ok, error) = await _email.SendTemplatedAsync(
                toEmail: trainer.Email,
                subject: request.Subject,
                templateKey: "admin-to-trainer",
                language: lang,
                placeholders: new Dictionary<string, string>
                {
                    ["TrainerName"] = trainer.FullName ?? trainer.Email,
                    ["AdminName"] = SenderName,
                    ["Subject"] = request.Subject,
                    ["Body"] = request.Body
                },
                replyTo: admin.Email,
                replyToName: SenderName);

            if (ok)
                result.Sent.Add(trainer.Email);
            else
                result.Failed.Add(new EmailFailureDto
                {
                    TrainerId = trainerId,
                    Email = trainer.Email,
                    Error = error ?? "Slanje neuspjelo."
                });
        }

        return Result<EmailSendResultDto>.Success(result);
    }
}
