using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Application.Features.Devices.Commands;

public record RegisterDeviceCommand(string Token, string Platform, string UserAgent) : IRequest<Result>;

public class RegisterDeviceCommandHandler : IRequestHandler<RegisterDeviceCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public RegisterDeviceCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(RegisterDeviceCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
            return Result.Fail(ResultError.Validation, "Token is required.");

        var userAgent = request.UserAgent.Length > 500 ? request.UserAgent[..500] : request.UserAgent;
        var userId = _currentUser.UserId;

        var existing = await _db.Devices.FirstOrDefaultAsync(t => t.Token == request.Token, cancellationToken);
        if (existing is null)
        {
            _db.Devices.Add(new Device
            {
                UserId = userId,
                Token = request.Token,
                Platform = string.IsNullOrWhiteSpace(request.Platform) ? "web" : request.Platform,
                UserAgent = userAgent,
                IsActive = true
            });
        }
        else
        {
            existing.UserId = userId;
            existing.Platform = string.IsNullOrWhiteSpace(request.Platform) ? existing.Platform : request.Platform;
            existing.UserAgent = userAgent;
            existing.LastSeenAt = DateTime.UtcNow;
            existing.IsActive = true;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
