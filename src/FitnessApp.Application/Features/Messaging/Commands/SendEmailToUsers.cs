using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.DTOs.Email;
using FitnessApp.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FitnessApp.Application.Features.Messaging.Commands;

public record SendEmailToUsersCommand(
    IReadOnlyList<string> UserIds,
    string Subject,
    string Body) : IRequest<Result<MessageSendResultDto>>;

public class SendEmailToUsersCommandHandler : IRequestHandler<SendEmailToUsersCommand, Result<MessageSendResultDto>>
{
    private readonly IIdentityAdminService _identity;
    private readonly ICurrentUserService _currentUser;
    private readonly IEmailService _email;
    private readonly ILogger<SendEmailToUsersCommandHandler> _logger;

    public SendEmailToUsersCommandHandler(
        IIdentityAdminService identity,
        ICurrentUserService currentUser,
        IEmailService email,
        ILogger<SendEmailToUsersCommandHandler> logger)
    {
        _identity = identity;
        _currentUser = currentUser;
        _email = email;
        _logger = logger;
    }

    public async Task<Result<MessageSendResultDto>> Handle(SendEmailToUsersCommand request, CancellationToken cancellationToken)
    {
        if (!_email.IsConfigured)
            return Result<MessageSendResultDto>.Fail(ResultError.Validation, "Email nije konfiguriran.");

        var admin = await _identity.FindByIdAsync(_currentUser.UserId, cancellationToken);
        var adminName = admin?.FullName ?? admin?.Email ?? "Admin";
        var result = new MessageSendResultDto();

        foreach (var userId in request.UserIds.Distinct())
        {
            var user = await _identity.FindByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                result.Failed.Add(new MessageFailureDto { UserId = userId, Error = "Korisnik nije pronađen." });
                _logger.LogWarning("User {UserId} not found, skipping.", userId);
                continue;
            }
            if (string.IsNullOrWhiteSpace(user.Email))
            {
                result.Failed.Add(new MessageFailureDto { UserId = userId, Recipient = user.FullName ?? "", Error = "Korisnik nema email adresu." });
                _logger.LogWarning("User {UserId} does not have an email address, skipping.", userId);
                continue;
            }

            try
            {
                await _email.SendAsync(user.Email, request.Subject, request.Body, replyTo: admin?.Email, replyToName: adminName);
                result.Sent.Add(user.Email);
            }
            catch (Exception ex)
            {
                result.Failed.Add(new MessageFailureDto { UserId = userId, Recipient = user.Email, Error = ex.Message });
            }
        }

        return Result<MessageSendResultDto>.Success(result);
    }
}
