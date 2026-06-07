using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.DTOs.Trainers;
using FitnessApp.Application.Interfaces;
using FitnessApp.Domain.Common;
using FitnessApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FitnessApp.Application.Features.TrainerRequests.Commands;

public record SendTrainerRequestCommand(string TrainerId, string? Language, string BaseUrl) : IRequest<Result<MyTrainerRequestDto>>;

public class SendTrainerRequestCommandHandler : IRequestHandler<SendTrainerRequestCommand, Result<MyTrainerRequestDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUserDirectory _users;
    private readonly IUserRelationshipService _relationship;
    private readonly IEmailService _email;
    private readonly ILogger<SendTrainerRequestCommandHandler> _logger;

    public SendTrainerRequestCommandHandler(
        IAppDbContext db,
        ICurrentUserService currentUser,
        IUserDirectory users,
        IUserRelationshipService relationship,
        IEmailService email,
        ILogger<SendTrainerRequestCommandHandler> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _users = users;
        _relationship = relationship;
        _email = email;
        _logger = logger;
    }

    public async Task<Result<MyTrainerRequestDto>> Handle(SendTrainerRequestCommand request, CancellationToken cancellationToken)
    {
        var client = await _users.FindAsync(_currentUser.UserId, cancellationToken);
        if (client == null) return Result<MyTrainerRequestDto>.NotFound();
        if (!string.IsNullOrEmpty(client.TrainerId))
            return Result<MyTrainerRequestDto>.Fail(ResultError.Validation, "You already have a trainer. Disconnect first before requesting another.");

        var trainer = await _users.FindAsync(request.TrainerId, cancellationToken);
        if (trainer == null || !await _relationship.IsInRoleAsync(request.TrainerId, Roles.Trainer, cancellationToken))
            return Result<MyTrainerRequestDto>.Fail(ResultError.Validation, "Selected trainer not found.");

        var hasPending = await _db.TrainerRequests
            .AnyAsync(r => r.ClientId == client.Id && r.Status == TrainerRequestStatus.Pending, cancellationToken);
        if (hasPending)
            return Result<MyTrainerRequestDto>.Fail(ResultError.Validation, "You already have a pending request. Cancel it first.");

        var entity = new TrainerRequest
        {
            ClientId = client.Id,
            TrainerId = trainer.Id,
            Status = TrainerRequestStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        _db.TrainerRequests.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        await TryNotifyTrainerAsync(trainer, client, request.Language, request.BaseUrl);

        return Result<MyTrainerRequestDto>.Success(new MyTrainerRequestDto
        {
            Id = entity.Id,
            TrainerId = trainer.Id,
            TrainerName = trainer.DisplayName,
            TrainerImageUrl = trainer.ProfileImageUrl,
            Status = entity.Status.ToString(),
            CreatedAt = entity.CreatedAt,
            RespondedAt = entity.RespondedAt
        });
    }

    // Best-effort email to the trainer. Never blocks: if SMTP is unconfigured or sending fails, log and move on.
    private async Task TryNotifyTrainerAsync(UserInfo trainer, UserInfo client, string? language, string baseUrl)
    {
        if (!_email.IsConfigured || string.IsNullOrWhiteSpace(trainer.Email))
            return;

        var lang = (language ?? "hr").ToLowerInvariant();
        var clientName = client.DisplayName;
        var subject = lang == "en"
            ? "New coaching request — FitnessApp"
            : "Novi zahtjev za suradnju — FitnessApp";

        try
        {
            var (ok, error) = await _email.SendTemplatedAsync(
                toEmail: trainer.Email,
                subject: subject,
                templateKey: "trainer-request",
                language: lang,
                placeholders: new Dictionary<string, string>
                {
                    ["TrainerName"] = trainer.DisplayName,
                    ["ClientName"] = clientName,
                    ["LoginUrl"] = baseUrl + "index.html"
                },
                replyTo: client.Email,
                replyToName: clientName);

            if (!ok)
                _logger.LogWarning("Trainer-request email to {Email} failed: {Error}", trainer.Email, error);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Trainer-request email to {Email} threw.", trainer.Email);
        }
    }
}
