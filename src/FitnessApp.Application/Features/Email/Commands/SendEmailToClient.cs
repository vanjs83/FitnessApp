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
    private readonly IMessageScheduler _scheduler;

    public SendEmailToClientCommandHandler(ICurrentUserService currentUser, IUserDirectory users, IMessageScheduler scheduler)
    {
        _currentUser = currentUser;
        _users = users;
        _scheduler = scheduler;
    }

    public async Task<Result<EmailSendStatusDto>> Handle(SendEmailToClientCommand request, CancellationToken cancellationToken)
    {
        var resolved = await EmailRecipients.ResolveAsync(_users, _currentUser.UserId, request.ClientId, cancellationToken);
        if (!resolved.Succeeded) return Result<EmailSendStatusDto>.Fail(resolved.Error, resolved.Message);
        var (trainer, client) = resolved.Value!;

        var lang = (request.Language ?? "hr").ToLowerInvariant();
        var trainerName = trainer.FullName ?? trainer.Email!;

        var placeholders = new Dictionary<string, string>
        {
            ["Body"] = request.Body,
            ["TrainerName"] = trainerName
        };
        _scheduler.Schedule<IEmailService>(m => m.SendTemplatedAsync(
            client.Email!, request.Subject, "trainer-to-client", lang, placeholders, trainer.Email, trainerName));

        // Fire-and-forget: the mail is queued; delivery is no longer awaited.
        return Result<EmailSendStatusDto>.Success(new EmailSendStatusDto(true, client.Email, trainer.Email));
    }
}
