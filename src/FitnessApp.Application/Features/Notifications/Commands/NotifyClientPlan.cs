using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Application.Features.Notifications.Commands;

public record NotifyResultDto(bool Sent, int ActiveTokens);

public record NotifyClientPlanCommand(string ClientId, string PlanName, string PlanType, string? Language)
    : IRequest<Result<NotifyResultDto>>;

public class NotifyClientPlanCommandHandler : IRequestHandler<NotifyClientPlanCommand, Result<NotifyResultDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUserDirectory _users;
    private readonly IMessageScheduler _scheduler;

    public NotifyClientPlanCommandHandler(
        IAppDbContext db, ICurrentUserService currentUser, IUserDirectory users, IMessageScheduler scheduler)
    {
        _db = db;
        _currentUser = currentUser;
        _users = users;
        _scheduler = scheduler;
    }

    public async Task<Result<NotifyResultDto>> Handle(NotifyClientPlanCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ClientId) || string.IsNullOrWhiteSpace(request.PlanName))
            return Result<NotifyResultDto>.Fail(ResultError.Validation, "ClientId and PlanName are required.");

        var client = await _users.FindAsync(request.ClientId, cancellationToken);
        if (client is null) return Result<NotifyResultDto>.NotFound("Client not found.");
        if (client.TrainerId != _currentUser.UserId) return Result<NotifyResultDto>.Forbidden();

        var lang = (request.Language ?? "hr").ToLowerInvariant();
        var (title, body) = (request.PlanType, lang) switch
        {
            ("nutrition", "en") => ("Nutrition plan ready", $"Your new nutrition plan '{request.PlanName}' is ready."),
            ("nutrition", _) => ("Plan prehrane spreman", $"Tvoj novi plan prehrane '{request.PlanName}' je spreman."),
            (_, "en") => ("Training plan ready", $"Your new training plan '{request.PlanName}' is ready."),
            _ => ("Plan treninga spreman", $"Tvoj novi plan treninga '{request.PlanName}' je spreman.")
        };

        var data = new Dictionary<string, string>
        {
            ["planType"] = request.PlanType,
            ["planName"] = request.PlanName
        };
        var clientId = client.Id;
        _scheduler.Schedule<IPushNotificationService>(p => p.SendToUserAsync(clientId, title, body, data));

        var activeTokens = await _db.Devices.CountAsync(t => t.UserId == client.Id && t.IsActive, cancellationToken);
        return Result<NotifyResultDto>.Success(new NotifyResultDto(true, activeTokens));
    }
}
