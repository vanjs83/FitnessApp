using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.DTOs.Messaging;
using FitnessApp.Application.Features.Groups;
using FitnessApp.Application.Features.Trainers;
using FitnessApp.Domain.Common;
using FitnessApp.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Application.Features.ScheduledMessages.Commands;

public record ScheduleMessageRequest(
    IReadOnlyList<ScheduledMessageChannel> Channels,
    ScheduledMessageAudience Audience,
    string? UserId,
    int? GroupId,
    string Subject,
    string Body,
    DateTime SendAtUtc);

public class ScheduleMessageRequestValidator : AbstractValidator<ScheduleMessageRequest>
{
    public ScheduleMessageRequestValidator()
    {
        RuleFor(x => x.Channels).NotEmpty().WithMessage("At least one channel is required.");
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Body).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.SendAtUtc)
            .GreaterThan(_ => DateTime.UtcNow).WithMessage("The send time must be in the future.");
        RuleFor(x => x.UserId)
            .NotEmpty().When(x => x.Audience == ScheduledMessageAudience.Single)
            .WithMessage("UserId is required when the audience is a single user.");
        RuleFor(x => x.GroupId)
            .NotNull().When(x => x.Audience == ScheduledMessageAudience.Group)
            .WithMessage("GroupId is required when the audience is a group.");
    }
}

public record ScheduleMessageCommand(
    IReadOnlyList<ScheduledMessageChannel> Channels,
    ScheduledMessageAudience Audience,
    string? UserId,
    int? GroupId,
    string Subject,
    string Body,
    DateTime SendAtUtc) : IRequest<Result<IReadOnlyList<ScheduledMessageDto>>>;

public class ScheduleMessageCommandHandler : IRequestHandler<ScheduleMessageCommand, Result<IReadOnlyList<ScheduledMessageDto>>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUserDirectory _users;

    public ScheduleMessageCommandHandler(IAppDbContext db, ICurrentUserService currentUser, IUserDirectory users)
    {
        _db = db;
        _currentUser = currentUser;
        _users = users;
    }

    public async Task<Result<IReadOnlyList<ScheduledMessageDto>>> Handle(ScheduleMessageCommand request, CancellationToken cancellationToken)
    {
        if (request.Channels is not { Count: > 0 })
            return Result<IReadOnlyList<ScheduledMessageDto>>.Fail(ResultError.Validation, "At least one channel is required.");

        if (request.SendAtUtc <= DateTime.UtcNow)
            return Result<IReadOnlyList<ScheduledMessageDto>>.Fail(ResultError.Validation, "The send time must be in the future.");

        var senderId = _currentUser.UserId;
        var isAdmin = _currentUser.IsInRole(Roles.SuperAdmin);

        // Trainers may only target their own client / group; an admin may target any existing one.
        if (request.Audience == ScheduledMessageAudience.Single)
        {
            if (string.IsNullOrWhiteSpace(request.UserId))
                return Result<IReadOnlyList<ScheduledMessageDto>>.Fail(ResultError.Validation, "A recipient user is required.");

            if (isAdmin)
            {
                if (await _users.FindAsync(request.UserId, cancellationToken) is null)
                    return Result<IReadOnlyList<ScheduledMessageDto>>.NotFound("Client not found.");
            }
            else
            {
                var error = await TrainerGuard.CheckOwnClientAsync(_users, request.UserId, senderId, cancellationToken);
                if (error is not null) return Result<IReadOnlyList<ScheduledMessageDto>>.Fail(error.Value);
            }
        }
        else
        {
            if (request.GroupId is not int groupId)
                return Result<IReadOnlyList<ScheduledMessageDto>>.Fail(ResultError.Validation, "A group is required.");

            if (isAdmin)
            {
                if (!await _db.TrainingGroups.AnyAsync(g => g.Id == groupId, cancellationToken))
                    return Result<IReadOnlyList<ScheduledMessageDto>>.NotFound("Group not found.");
            }
            else
            {
                var (_, error) = await GroupGuard.LoadOwnedAsync(_db, groupId, senderId, cancellationToken);
                if (error is not null) return Result<IReadOnlyList<ScheduledMessageDto>>.Fail(error.Value);
            }
        }

        // One row per selected channel so the due-job can dispatch each channel independently.
        var now = DateTime.UtcNow;
        var entities = request.Channels.Distinct().Select(channel => new ScheduledMessage
        {
            SenderId = senderId,
            Channel = channel,
            Audience = request.Audience,
            UserId = request.Audience == ScheduledMessageAudience.Single ? request.UserId : null,
            GroupId = request.Audience == ScheduledMessageAudience.Group ? request.GroupId : null,
            Subject = request.Subject,
            Body = request.Body,
            SendAtUtc = request.SendAtUtc,
            Status = ScheduledMessageStatus.Pending,
            CreatedAtUtc = now
        }).ToList();

        _db.ScheduledMessages.AddRange(entities);
        await _db.SaveChangesAsync(cancellationToken);

        return Result<IReadOnlyList<ScheduledMessageDto>>.Success(entities.Select(e => e.ToDto()).ToList());
    }
}
