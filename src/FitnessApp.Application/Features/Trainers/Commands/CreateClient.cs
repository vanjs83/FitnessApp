using System.Security.Cryptography;
using System.Text;
using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.DTOs.Trainers;
using FitnessApp.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FitnessApp.Application.Features.Trainers.Commands;

public record CreateClientCommand(string Email, string? FullName, string? Language, string BaseUrl) : IRequest<Result<ClientListItemDto>>;

public class CreateClientCommandHandler : IRequestHandler<CreateClientCommand, Result<ClientListItemDto>>
{
    private readonly IIdentityAdminService _identity;
    private readonly IUserRelationshipService _relationship;
    private readonly ICurrentUserService _currentUser;
    private readonly IUserDirectory _users;
    private readonly IEmailService _email;
    private readonly ILogger<CreateClientCommandHandler> _logger;

    public CreateClientCommandHandler(
        IIdentityAdminService identity,
        IUserRelationshipService relationship,
        ICurrentUserService currentUser,
        IUserDirectory users,
        IEmailService email,
        ILogger<CreateClientCommandHandler> logger)
    {
        _identity = identity;
        _relationship = relationship;
        _currentUser = currentUser;
        _users = users;
        _email = email;
        _logger = logger;
    }

    public async Task<Result<ClientListItemDto>> Handle(CreateClientCommand request, CancellationToken cancellationToken)
    {
        if (await _identity.EmailExistsAsync(request.Email, cancellationToken))
            return Result<ClientListItemDto>.Fail(ResultError.Validation, "A user with this email already exists.");

        if (!_email.IsConfigured)
            return Result<ClientListItemDto>.Fail(ResultError.Validation, "SMTP is not configured. Client was not created.");

        var trainer = await _users.FindAsync(_currentUser.UserId, cancellationToken);
        if (trainer == null || string.IsNullOrWhiteSpace(trainer.Email))
            return Result<ClientListItemDto>.Fail(ResultError.Validation, "Trainer has no email in profile. Client was not created.");

        var tempPassword = GenerateTempPassword();

        if (!await TrySendWelcomeEmailAsync(request, trainer, tempPassword))
            return Result<ClientListItemDto>.Fail(ResultError.Validation,
                "Sending email failed. Client was not created — check the email address and try again.");

        var (succeeded, user, errors) = await _relationship.CreateClientAsync(
            request.Email, request.FullName, tempPassword, _currentUser.UserId, cancellationToken);
        if (!succeeded || user == null)
        {
            _logger.LogError("Welcome email was sent to {Email} but user creation failed: {Errors}",
                request.Email, string.Join("; ", errors));
            return Result<ClientListItemDto>.Fail(ResultError.Validation, string.Join(", ", errors));
        }

        return Result<ClientListItemDto>.Success(new ClientListItemDto
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            CreatedAt = user.CreatedAt,
            PlanCount = 0,
            PerformedSetCount = 0,
            ProfileImageUrl = user.ProfileImageUrl
        });
    }

    private async Task<bool> TrySendWelcomeEmailAsync(CreateClientCommand request, UserInfo trainer, string tempPassword)
    {
        var trainerName = trainer.DisplayName;
        var lang = (request.Language ?? "hr").ToLowerInvariant();

        var greeting = lang == "en"
            ? (string.IsNullOrWhiteSpace(request.FullName) ? "Hi" : $"Hi {request.FullName}")
            : (string.IsNullOrWhiteSpace(request.FullName) ? "Bok" : $"Bok {request.FullName}");

        var subject = lang == "en"
            ? "Welcome to FitnessApp — your sign-in details"
            : "Dobrodošao u FitnessApp — tvoji pristupni podaci";

        var (ok, _) = await _email.SendTemplatedAsync(
            toEmail: request.Email,
            subject: subject,
            templateKey: "welcome-client",
            language: lang,
            placeholders: new Dictionary<string, string>
            {
                ["Greeting"] = greeting,
                ["TrainerName"] = trainerName,
                ["Email"] = request.Email,
                ["Password"] = tempPassword,
                ["LoginUrl"] = request.BaseUrl
            },
            replyTo: trainer.Email,
            replyToName: trainerName);

        return ok;
    }

    private static string GenerateTempPassword(int length = 10)
    {
        const string chars = "abcdefghijkmnpqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var bytes = RandomNumberGenerator.GetBytes(length);
        var sb = new StringBuilder(length);
        for (int i = 0; i < length; i++)
            sb.Append(chars[bytes[i] % chars.Length]);
        return sb.ToString();
    }
}
