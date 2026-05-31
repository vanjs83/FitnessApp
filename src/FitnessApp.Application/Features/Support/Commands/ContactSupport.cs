using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace FitnessApp.Application.Features.Support.Commands;

public record ContactSupportCommand(string Subject, string Body, string? Language) : IRequest<Result>;

public class ContactSupportCommandHandler : IRequestHandler<ContactSupportCommand, Result>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IUserDirectory _users;
    private readonly IEmailService _email;
    private readonly IConfiguration _config;

    public ContactSupportCommandHandler(
        ICurrentUserService currentUser, IUserDirectory users, IEmailService email, IConfiguration config)
    {
        _currentUser = currentUser;
        _users = users;
        _email = email;
        _config = config;
    }

    public async Task<Result> Handle(ContactSupportCommand request, CancellationToken cancellationToken)
    {
        var supportEmail = _config["Smtp:FromEmail"];
        if (string.IsNullOrWhiteSpace(supportEmail))
            return Result.Fail(ResultError.Validation, "Customer support is not configured yet.");

        var user = await _users.FindAsync(_currentUser.UserId, cancellationToken);
        if (user == null || string.IsNullOrWhiteSpace(user.Email))
            return Result.Fail(ResultError.Validation, "Your account email is not available.");

        var role = _currentUser.PrimaryRole ?? "User";
        var userName = user.FullName ?? user.Email!;
        var lang = (request.Language ?? "hr").ToLowerInvariant();
        var subjectPrefix = lang == "en" ? "[FitnessApp support]" : "[FitnessApp podrška]";

        var (ok, error) = await _email.SendTemplatedAsync(
            toEmail: supportEmail,
            subject: $"{subjectPrefix} {request.Subject}",
            templateKey: "support-message",
            language: lang,
            placeholders: new Dictionary<string, string>
            {
                ["UserName"] = userName,
                ["UserEmail"] = user.Email!,
                ["UserRole"] = role,
                ["Subject"] = request.Subject,
                ["Body"] = request.Body
            },
            replyTo: user.Email,
            replyToName: userName);

        if (!ok)
            return Result.Fail(ResultError.Validation, error ?? "Sending the message failed. Try again later.");

        return Result.Success();
    }
}
