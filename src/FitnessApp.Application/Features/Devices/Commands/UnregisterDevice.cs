using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Application.Features.Devices.Commands;

public record UnregisterDeviceCommand(string Token) : IRequest<Result>;

public class UnregisterDeviceCommandHandler : IRequestHandler<UnregisterDeviceCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UnregisterDeviceCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(UnregisterDeviceCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.Devices
            .FirstOrDefaultAsync(t => t.Token == request.Token && t.UserId == _currentUser.UserId, cancellationToken);
        if (entity is null) return Result.NotFound();

        entity.IsActive = false;
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
