using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.DTOs.Auth;
using FitnessApp.Domain.Common;
using MediatR;

namespace FitnessApp.Application.Features.Trainers.Queries;

// ===== Public trainer profile (any authenticated user) =====

public record GetTrainerProfileQuery(string TrainerId) : IRequest<Result<PersonalProfileDto>>;

public class GetTrainerProfileQueryHandler : IRequestHandler<GetTrainerProfileQuery, Result<PersonalProfileDto>>
{
    private readonly IUserProfileService _profiles;

    public GetTrainerProfileQueryHandler(IUserProfileService profiles) => _profiles = profiles;

    public async Task<Result<PersonalProfileDto>> Handle(GetTrainerProfileQuery request, CancellationToken cancellationToken)
    {
        var profile = await _profiles.GetProfileAsync(request.TrainerId, cancellationToken);
        if (profile == null) return Result<PersonalProfileDto>.NotFound("Trainer not found.");
        if (profile.Role != Roles.Trainer)
            return Result<PersonalProfileDto>.Fail(ResultError.Validation, "User is not a trainer.");
        return Result<PersonalProfileDto>.Success(profile);
    }
}

// ===== A trainer reading one of their clients' profile =====

public record GetClientProfileQuery(string ClientId) : IRequest<Result<PersonalProfileDto>>;

public class GetClientProfileQueryHandler : IRequestHandler<GetClientProfileQuery, Result<PersonalProfileDto>>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IUserDirectory _users;
    private readonly IUserProfileService _profiles;

    public GetClientProfileQueryHandler(ICurrentUserService currentUser, IUserDirectory users, IUserProfileService profiles)
    {
        _currentUser = currentUser;
        _users = users;
        _profiles = profiles;
    }

    public async Task<Result<PersonalProfileDto>> Handle(GetClientProfileQuery request, CancellationToken cancellationToken)
    {
        var error = await TrainerGuard.CheckOwnClientAsync(_users, request.ClientId, _currentUser.UserId, cancellationToken);
        if (error != null) return Result<PersonalProfileDto>.Fail(error.Value);

        var profile = await _profiles.GetProfileAsync(request.ClientId, cancellationToken);
        if (profile == null) return Result<PersonalProfileDto>.NotFound("Client not found.");
        return Result<PersonalProfileDto>.Success(profile);
    }
}
