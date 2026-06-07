using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FitnessApp.Application.Features.Auth.Commands;

// ===== Forgot password: email a reset link (always succeeds to avoid account probing) =====

public record ForgotPasswordCommand(string Email, string? Language, string BaseUrl) : IRequest<Result>;

public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, Result>
{
    private readonly IAuthService _auth;
    private readonly IEmailService _email;
    private readonly ILogger<ForgotPasswordCommandHandler> _logger;

    public ForgotPasswordCommandHandler(IAuthService auth, IEmailService email, ILogger<ForgotPasswordCommandHandler> logger)
    {
        _auth = auth;
        _email = email;
        _logger = logger;
    }

    public async Task<Result> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return Result.Success();

        var (token, email, fullName) = await _auth.CreatePasswordResetTokenAsync(request.Email, cancellationToken);
        if (token == null || email == null)
            return Result.Success();

        // Reset link must target index.html explicitly: "/" serves landing.html (no auth logic).
        var resetUrl = $"{request.BaseUrl}/index.html?reset={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(email)}";

        var (ok, error) = await _email.SendTemplatedAsync(
            toEmail: email,
            subject: "FitnessApp – reset lozinke",
            templateKey: "password-reset",
            language: request.Language,
            placeholders: new Dictionary<string, string>
            {
                ["Name"] = fullName ?? email,
                ["ResetUrl"] = resetUrl
            });

        if (!ok)
            _logger.LogError("Failed to send password-reset email to {Email}: {Error}", email, error);

        return Result.Success();
    }
}

// ===== Reset password: consume the token and set a new password =====

public record ResetPasswordCommand(string Email, string Token, string NewPassword, string? Language) : IRequest<Result>;

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Result>
{
    private readonly IAuthService _auth;
    private readonly IEmailService _email;
    private readonly ILogger<ResetPasswordCommandHandler> _logger;

    public ResetPasswordCommandHandler(IAuthService auth, IEmailService email, ILogger<ResetPasswordCommandHandler> logger)
    {
        _auth = auth;
        _email = email;
        _logger = logger;
    }

    public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Token))
            return Result.Fail(ResultError.Validation, "The reset link is invalid or has expired.");

        var (succeeded, errors, email, fullName) = await _auth.ResetPasswordAsync(
            request.Email, request.Token, request.NewPassword, cancellationToken);
        if (!succeeded)
            return Result.Fail(ResultError.Validation, string.Join(", ", errors));

        // Confirmation mail is best-effort — a delivery failure must not fail the reset.
        var (ok, error) = await _email.SendTemplatedAsync(
            toEmail: email!,
            subject: "FitnessApp – lozinka promijenjena",
            templateKey: "password-changed",
            language: request.Language,
            placeholders: new Dictionary<string, string> { ["Name"] = fullName ?? email! });

        if (!ok)
            _logger.LogWarning("Password reset succeeded but confirmation email failed for {Email}: {Error}", email, error);

        return Result.Success();
    }
}
