using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.DTOs.Email;
using FitnessApp.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FitnessApp.Application.Features.Messaging.Commands;

public record SendPushToUsersCommand(
    IReadOnlyList<string> UserIds,
    string Subject,
    string Body) : IRequest<Result<MessageSendResultDto>>;

public class SendPushToUsersCommandHandler : IRequestHandler<SendPushToUsersCommand, Result<MessageSendResultDto>>
{
    private readonly IIdentityAdminService _identity;
    private readonly IAppDbContext _db;
    private readonly IPushNotificationService _push;
    private readonly ILogger<SendPushToUsersCommandHandler> _logger;

    public SendPushToUsersCommandHandler(
        IIdentityAdminService identity,
        IAppDbContext db,
        IPushNotificationService push,
        ILogger<SendPushToUsersCommandHandler> logger)
    {
        _identity = identity;
        _db = db;
        _push = push;
        _logger = logger;
    }

    public async Task<Result<MessageSendResultDto>> Handle(SendPushToUsersCommand request, CancellationToken cancellationToken)
    {
        var result = new MessageSendResultDto();

        foreach (var userId in request.UserIds.Distinct())
        {
            var user = await _identity.FindByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                result.Failed.Add(new MessageFailureDto { UserId = userId, Error = "Korisnik nije pronađen." });
                _logger.LogWarning("User {UserId} not found, skipping push notification.", userId);
                continue;
            }

            var recipient = user.FullName ?? user.Email ?? userId;
            var hasDevice = await _db.Devices.AnyAsync(d => d.UserId == userId && d.IsActive, cancellationToken);

            try
            {
                await _push.SendToUserAsync(userId, request.Subject, request.Body, ct: cancellationToken);
                result.Sent.Add(recipient);
            }
            catch (Exception ex)
            {
                result.Failed.Add(new MessageFailureDto { UserId = userId, Recipient = recipient, Error = ex.Message });
                _logger.LogError(ex, "Failed to send push notification to user {UserId} ({Recipient}). Has device: {HasDevice}", userId, recipient, hasDevice);
            }
        }

        return Result<MessageSendResultDto>.Success(result);
    }
}
