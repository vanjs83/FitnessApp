using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.DTOs.Auth;
using MediatR;

namespace FitnessApp.Application.Features.Auth.Queries;

// ===== Public Google client id for the SPA =====

public record GetGoogleConfigQuery : IRequest<string?>;

public class GetGoogleConfigQueryHandler : IRequestHandler<GetGoogleConfigQuery, string?>
{
    private readonly IAuthService _auth;
    public GetGoogleConfigQueryHandler(IAuthService auth) => _auth = auth;

    public Task<string?> Handle(GetGoogleConfigQuery request, CancellationToken cancellationToken)
        => Task.FromResult(_auth.GoogleClientId);
}

// ===== Current user summary (header) =====

public record GetMeQuery : IRequest<MeResponse?>;

public class GetMeQueryHandler : IRequestHandler<GetMeQuery, MeResponse?>
{
    private readonly IAuthService _auth;
    private readonly ICurrentUserService _currentUser;

    public GetMeQueryHandler(IAuthService auth, ICurrentUserService currentUser)
    {
        _auth = auth;
        _currentUser = currentUser;
    }

    public Task<MeResponse?> Handle(GetMeQuery request, CancellationToken cancellationToken)
        => _auth.GetMeAsync(_currentUser.UserId, cancellationToken);
}

// ===== Full personal profile (own) =====

public record GetPersonalProfileQuery : IRequest<PersonalProfileDto?>;

public class GetPersonalProfileQueryHandler : IRequestHandler<GetPersonalProfileQuery, PersonalProfileDto?>
{
    private readonly IUserProfileService _profiles;
    private readonly ICurrentUserService _currentUser;

    public GetPersonalProfileQueryHandler(IUserProfileService profiles, ICurrentUserService currentUser)
    {
        _profiles = profiles;
        _currentUser = currentUser;
    }

    public Task<PersonalProfileDto?> Handle(GetPersonalProfileQuery request, CancellationToken cancellationToken)
        => _profiles.GetProfileAsync(_currentUser.UserId, cancellationToken);
}
