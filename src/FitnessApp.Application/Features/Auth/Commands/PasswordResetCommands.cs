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
    private readonly IMessageScheduler _scheduler;
    private readonly ILogger<ForgotPasswordCommandHandler> _logger;

    public ForgotPasswordCommandHandler(IAuthService auth, IMessageScheduler scheduler, ILogger<ForgotPasswordCommandHandler> logger)
    {
        _auth = auth;
        _scheduler = scheduler;
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

        var placeholders = new Dictionary<string, string>
        {
            ["Name"] = fullName ?? email,
            ["ResetUrl"] = resetUrl
        };
        _scheduler.Schedule<IEmailService>(async m =>
        {
            var (ok, error) = await m.SendTemplatedAsync(
                email, "FitnessApp – reset lozinke", "password-reset", request.Language, placeholders);
            if (!ok)
                _logger.LogError("Failed to send password-reset email to {Email}: {Error}", email, error);
        });

        return Result.Success();
    }
}

// ===== Reset password: consume the token and set a new password =====

public record ResetPasswordCommand(string Email, string Token, string NewPassword, string? Language) : IRequest<Result>;

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Result>
{
    private readonly IAuthService _auth;
    private readonly IMessageScheduler _scheduler;
    private readonly ILogger<ResetPasswordCommandHandler> _logger;

    public ResetPasswordCommandHandler(IAuthService auth, IMessageScheduler scheduler, ILogger<ResetPasswordCommandHandler> logger)
    {
        _auth = auth;
        _scheduler = scheduler;
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

        // Confirmation mail is best-effort — queued fire-and-forget so it never delays the reset.
        var toEmail = email!;
        var placeholders = new Dictionary<string, string> { ["Name"] = fullName ?? email! };
        _scheduler.Schedule<IEmailService>(async m =>
        {
            var (ok, error) = await m.SendTemplatedAsync(
                toEmail, "FitnessApp – lozinka promijenjena", "password-changed", request.Language, placeholders);
            if (!ok)
                _logger.LogWarning("Confirmation email failed for {Email}: {Error}", toEmail, error);
        });

        return Result.Success();
    }
}
