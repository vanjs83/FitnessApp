using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.DTOs.Chat;
using FitnessApp.Domain.Entities;
using MediatR;

namespace FitnessApp.Application.Features.Chat.Commands;

public record SendMessageCommand(string PartnerId, string? Body) : IRequest<Result<ChatMessageDto>>;

public class SendMessageCommandHandler : IRequestHandler<SendMessageCommand, Result<ChatMessageDto>>
{
    private const int MaxBodyLength = 2000;

    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUserDirectory _users;
    private readonly IChatNotifier _notifier;

    public SendMessageCommandHandler(
        IAppDbContext db,
        ICurrentUserService currentUser,
        IUserDirectory users,
        IChatNotifier notifier)
    {
        _db = db;
        _currentUser = currentUser;
        _users = users;
        _notifier = notifier;
    }

    public async Task<Result<ChatMessageDto>> Handle(SendMessageCommand request, CancellationToken cancellationToken)
    {
        var body = request.Body?.Trim();
        if (string.IsNullOrWhiteSpace(body))
            return Result<ChatMessageDto>.Fail(ResultError.Validation, "Message cannot be empty.");
        if (body.Length > MaxBodyLength)
            return Result<ChatMessageDto>.Fail(ResultError.Validation, $"Message is too long (max {MaxBodyLength} characters).");

        var meId = _currentUser.UserId;
        if (!await _users.AreLinkedAsync(meId, request.PartnerId, cancellationToken))
            return Result<ChatMessageDto>.Forbidden();

        var message = new ChatMessage
        {
            SenderId = meId,
            RecipientId = request.PartnerId,
            Body = body,
            SentAt = DateTime.UtcNow
        };
        _db.ChatMessages.Add(message);
        await _db.SaveChangesAsync(cancellationToken);

        var dto = new ChatMessageDto
        {
            Id = message.Id,
            SenderId = message.SenderId,
            Body = message.Body,
            SentAt = message.SentAt,
            ReadAt = message.ReadAt
        };

        // Real-time delivery to both ends (recipient sees it instantly; sender's other tabs stay in sync).
        await _notifier.NotifyMessageAsync(new[] { request.PartnerId, meId }, dto, cancellationToken);

        return Result<ChatMessageDto>.Success(dto);
    }
}
