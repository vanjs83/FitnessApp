using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.DTOs.Auth;
using FitnessApp.Domain.Common;
using MediatR;

namespace FitnessApp.Application.Features.Auth.Commands;

// ===== Register (email + password) =====

public record RegisterCommand(string Email, string? FullName, string Password, string Role) : IRequest<Result<AuthResponse>>;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<AuthResponse>>
{
    private readonly IAuthService _auth;

    public RegisterCommandHandler(IAuthService auth) => _auth = auth;

    public async Task<Result<AuthResponse>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        if (!Roles.SelfRegisterable.Contains(request.Role))
            return Result<AuthResponse>.Fail(ResultError.Validation, $"Invalid role. Allowed: {string.Join(", ", Roles.SelfRegisterable)}.");

        if (await _auth.EmailExistsAsync(request.Email, cancellationToken))
            return Result<AuthResponse>.Fail(ResultError.Validation, "A user with this email already exists.");

        var (succeeded, errors, response) = await _auth.RegisterAsync(
            request.Email, request.FullName, request.Password, request.Role, cancellationToken);
        if (!succeeded || response == null)
            return Result<AuthResponse>.Fail(ResultError.Validation, string.Join(", ", errors));

        return Result<AuthResponse>.Success(response);
    }
}

// ===== Login (returns a code the controller maps to 401 variants) =====

public record LoginResult(LoginResultCode Code, AuthResponse? Response);

public record LoginCommand(string Email, string Password) : IRequest<LoginResult>;

public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResult>
{
    private readonly IAuthService _auth;

    public LoginCommandHandler(IAuthService auth) => _auth = auth;

    public async Task<LoginResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var (code, response) = await _auth.LoginAsync(request.Email, request.Password, cancellationToken);
        return new LoginResult(code, response);
    }
}

// ===== Google sign-in / sign-up =====

public record GoogleLoginResult(bool Ok, bool Unauthorized, string? Error, AuthResponse? Response);

public record GoogleLoginCommand(string Credential, string? Role) : IRequest<GoogleLoginResult>;

public class GoogleLoginCommandHandler : IRequestHandler<GoogleLoginCommand, GoogleLoginResult>
{
    private readonly IAuthService _auth;

    public GoogleLoginCommandHandler(IAuthService auth) => _auth = auth;

    public async Task<GoogleLoginResult> Handle(GoogleLoginCommand request, CancellationToken cancellationToken)
    {
        var (ok, unauthorized, error, response) = await _auth.GoogleLoginAsync(request.Credential, request.Role, cancellationToken);
        return new GoogleLoginResult(ok, unauthorized, error, response);
    }
}
