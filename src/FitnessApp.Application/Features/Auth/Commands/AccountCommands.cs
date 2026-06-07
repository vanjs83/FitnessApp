using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.DTOs.Auth;
using MediatR;

namespace FitnessApp.Application.Features.Auth.Commands;

// ===== Update display name =====

public record UpdateProfileCommand(string? FullName) : IRequest<Result<string?>>;

public class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, Result<string?>>
{
    private readonly IAuthService _auth;
    private readonly ICurrentUserService _currentUser;

    public UpdateProfileCommandHandler(IAuthService auth, ICurrentUserService currentUser)
    {
        _auth = auth;
        _currentUser = currentUser;
    }

    public async Task<Result<string?>> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        var (succeeded, errors, fullName) = await _auth.UpdateFullNameAsync(_currentUser.UserId, request.FullName, cancellationToken);
        return succeeded
            ? Result<string?>.Success(fullName)
            : Result<string?>.Fail(ResultError.Validation, string.Join(", ", errors));
    }
}

// ===== Disconnect from trainer (connecting goes through trainer requests) =====

public record DisconnectTrainerCommand(string? RequestedTrainerId) : IRequest<Result>;

public class DisconnectTrainerCommandHandler : IRequestHandler<DisconnectTrainerCommand, Result>
{
    private readonly IAuthService _auth;
    private readonly ICurrentUserService _currentUser;

    public DisconnectTrainerCommandHandler(IAuthService auth, ICurrentUserService currentUser)
    {
        _auth = auth;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(DisconnectTrainerCommand request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.RequestedTrainerId))
            return Result.Fail(ResultError.Validation, "To connect to a trainer, send a request the trainer must accept.");

        var (succeeded, errors) = await _auth.DisconnectTrainerAsync(_currentUser.UserId, cancellationToken);
        return succeeded ? Result.Success() : Result.Fail(ResultError.Validation, string.Join(", ", errors));
    }
}

// ===== Change password =====

public record ChangePasswordCommand(string CurrentPassword, string NewPassword) : IRequest<Result>;

public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, Result>
{
    private readonly IAuthService _auth;
    private readonly ICurrentUserService _currentUser;

    public ChangePasswordCommandHandler(IAuthService auth, ICurrentUserService currentUser)
    {
        _auth = auth;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var (succeeded, errors) = await _auth.ChangePasswordAsync(
            _currentUser.UserId, request.CurrentPassword, request.NewPassword, cancellationToken);
        return succeeded ? Result.Success() : Result.Fail(ResultError.Validation, string.Join(", ", errors));
    }
}

// ===== Update full personal profile =====

public record UpdatePersonalProfileCommand(UpdatePersonalProfileRequest Request) : IRequest<Result>;

public class UpdatePersonalProfileCommandHandler : IRequestHandler<UpdatePersonalProfileCommand, Result>
{
    private readonly IAuthService _auth;
    private readonly ICurrentUserService _currentUser;

    public UpdatePersonalProfileCommandHandler(IAuthService auth, ICurrentUserService currentUser)
    {
        _auth = auth;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(UpdatePersonalProfileCommand request, CancellationToken cancellationToken)
    {
        var (succeeded, errors) = await _auth.UpdatePersonalProfileAsync(_currentUser.UserId, request.Request, cancellationToken);
        return succeeded ? Result.Success() : Result.Fail(ResultError.Validation, string.Join(", ", errors));
    }
}

// ===== Self-service account deletion (soft delete) =====

public record DeleteAccountCommand : IRequest<Result>;

public class DeleteAccountCommandHandler : IRequestHandler<DeleteAccountCommand, Result>
{
    private readonly IAuthService _auth;
    private readonly ICurrentUserService _currentUser;

    public DeleteAccountCommandHandler(IAuthService auth, ICurrentUserService currentUser)
    {
        _auth = auth;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(DeleteAccountCommand request, CancellationToken cancellationToken)
    {
        var outcome = await _auth.DeleteAccountAsync(_currentUser.UserId, cancellationToken);
        return outcome switch
        {
            DeleteAccountResultCode.NotFound => Result.NotFound(),
            DeleteAccountResultCode.IsAdmin => Result.Fail(ResultError.Validation, "The administrator account cannot be deleted."),
            _ => Result.Success()
        };
    }
}
