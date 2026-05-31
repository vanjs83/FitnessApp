using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.Interfaces;
using MediatR;

namespace FitnessApp.Application.Features.Devices.Commands;

public record SendTestNotificationCommand : IRequest<Result>;

public class SendTestNotificationCommandHandler : IRequestHandler<SendTestNotificationCommand, Result>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IPushNotificationService _push;

    public SendTestNotificationCommandHandler(ICurrentUserService currentUser, IPushNotificationService push)
    {
        _currentUser = currentUser;
        _push = push;
    }

    public async Task<Result> Handle(SendTestNotificationCommand request, CancellationToken cancellationToken)
    {
        await _push.SendToUserAsync(_currentUser.UserId, "FitnessApp test", "Push notifikacije rade!", ct: cancellationToken);
        return Result.Success();
    }
}
