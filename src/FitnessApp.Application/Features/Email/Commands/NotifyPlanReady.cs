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
    private readonly IMessageScheduler _scheduler;

    public NotifyPlanReadyCommandHandler(ICurrentUserService currentUser, IUserDirectory users, IMessageScheduler scheduler)
    {
        _currentUser = currentUser;
        _users = users;
        _scheduler = scheduler;
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

        var placeholders = new Dictionary<string, string>
        {
            ["ClientName"] = client.FullName ?? (lang == "en" ? "athlete" : "klijente"),
            ["TrainerName"] = trainerName,
            ["PlanLabel"] = planLabel,
            ["PlanName"] = request.PlanName,
            ["LoginUrl"] = request.BaseUrl
        };
        _scheduler.Schedule<IEmailService>(m => m.SendTemplatedAsync(
            client.Email!, subject, "plan-ready", lang, placeholders, trainer.Email, trainerName));

        // Fire-and-forget: the mail is queued; delivery is no longer awaited.
        return Result<EmailSendStatusDto>.Success(new EmailSendStatusDto(true, client.Email, null));
    }
}
