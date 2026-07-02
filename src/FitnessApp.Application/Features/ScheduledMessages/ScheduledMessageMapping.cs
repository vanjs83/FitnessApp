using FitnessApp.Application.DTOs.Messaging;
using FitnessApp.Domain.Entities;

namespace FitnessApp.Application.Features.ScheduledMessages;

internal static class ScheduledMessageMapping
{
    public static ScheduledMessageDto ToDto(this ScheduledMessage m) => new(
        m.Id,
        m.Channel.ToString(),
        m.Audience.ToString(),
        m.UserId,
        m.GroupId,
        m.Subject,
        m.Body,
        m.SendAtUtc,
        m.Status.ToString(),
        m.CreatedAtUtc,
        m.SentAtUtc);
}
