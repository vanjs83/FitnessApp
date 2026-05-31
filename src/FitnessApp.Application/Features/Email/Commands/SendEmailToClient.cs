using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.Interfaces;
using MediatR;

namespace FitnessApp.Application.Features.Email.Commands;

public record SendEmailToClientCommand(
    string ClientId,
    string Subject,
    string Body,
    string? Language) : IRequest<Result<EmailSendStatusDto>>;

public class SendEmailToClientCommandHandler : IRequestHandler<SendEmailToClientCommand, Result<EmailSendStatusDto>>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IUserDirectory _users;
    private readonly IEmailService _email;

    public SendEmailToClientCommandHandler(ICurrentUserService currentUser, IUserDirectory users, IEmailService email)
    {
        _currentUser = currentUser;
        _users = users;
        _email = email;
    }

    public async Task<Result<EmailSendStatusDto>> Handle(SendEmailToClientCommand request, CancellationToken cancellationToken)
    {
        var resolved = await EmailRecipients.ResolveAsync(_users, _currentUser.UserId, request.ClientId, cancellationToken);
        if (!resolved.Succeeded) return Result<EmailSendStatusDto>.Fail(resolved.Error, resolved.Message);
        var (trainer, client) = resolved.Value!;

        var lang = (request.Language ?? "hr").ToLowerInvariant();
        var trainerName = trainer.FullName ?? trainer.Email!;

        var (ok, error) = await _email.SendTemplatedAsync(
            toEmail: client.Email!,
            subject: request.Subject,
            templateKey: "trainer-to-client",
            language: lang,
            placeholders: new Dictionary<string, string>
            {
                ["Body"] = request.Body,
                ["TrainerName"] = trainerName
            },
            replyTo: trainer.Email,
            replyToName: trainerName);

        if (!ok) return Result<EmailSendStatusDto>.Fail(ResultError.Validation, $"Sending failed: {error}");
        return Result<EmailSendStatusDto>.Success(new EmailSendStatusDto(true, client.Email, trainer.Email));
    }
}
