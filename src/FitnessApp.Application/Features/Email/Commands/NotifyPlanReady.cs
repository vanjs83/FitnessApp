using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.Interfaces;
using MediatR;

namespace FitnessApp.Application.Features.Email.Commands;

public record NotifyPlanReadyCommand(
    string ClientId,
    string PlanName,
    string PlanType,
    string? Language,
    string BaseUrl) : IRequest<Result<EmailSendStatusDto>>;

public class NotifyPlanReadyCommandHandler : IRequestHandler<NotifyPlanReadyCommand, Result<EmailSendStatusDto>>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IUserDirectory _users;
    private readonly IEmailService _email;

    public NotifyPlanReadyCommandHandler(ICurrentUserService currentUser, IUserDirectory users, IEmailService email)
    {
        _currentUser = currentUser;
        _users = users;
        _email = email;
    }

    public async Task<Result<EmailSendStatusDto>> Handle(NotifyPlanReadyCommand request, CancellationToken cancellationToken)
    {
        var resolved = await EmailRecipients.ResolveAsync(_users, _currentUser.UserId, request.ClientId, cancellationToken);
        if (!resolved.Succeeded) return Result<EmailSendStatusDto>.Fail(resolved.Error, resolved.Message);
        var (trainer, client) = resolved.Value!;

        var lang = (request.Language ?? "hr").ToLowerInvariant();
        var trainerName = trainer.FullName ?? trainer.Email!;
        var planLabel = (request.PlanType, lang) switch
        {
            ("nutrition", "en") => "nutrition plan",
            ("nutrition", _) => "plan prehrane",
            (_, "en") => "training plan",
            _ => "plan treninga"
        };
        var subject = lang == "en"
            ? $"Your new {planLabel} is ready — {request.PlanName}"
            : $"Tvoj novi {planLabel} je spreman — {request.PlanName}";

        var (ok, error) = await _email.SendTemplatedAsync(
            toEmail: client.Email!,
            subject: subject,
            templateKey: "plan-ready",
            language: lang,
            placeholders: new Dictionary<string, string>
            {
                ["ClientName"] = client.FullName ?? (lang == "en" ? "athlete" : "klijente"),
                ["TrainerName"] = trainerName,
                ["PlanLabel"] = planLabel,
                ["PlanName"] = request.PlanName,
                ["LoginUrl"] = request.BaseUrl
            },
            replyTo: trainer.Email,
            replyToName: trainerName);

        if (!ok) return Result<EmailSendStatusDto>.Fail(ResultError.Validation, $"Sending failed: {error}");
        return Result<EmailSendStatusDto>.Success(new EmailSendStatusDto(true, client.Email, null));
    }
}
