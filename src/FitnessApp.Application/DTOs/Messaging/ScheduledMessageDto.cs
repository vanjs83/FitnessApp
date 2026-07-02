namespace FitnessApp.Application.DTOs.Messaging;

/// <summary>A scheduled message as returned to the trainer/admin who owns it.</summary>
public record ScheduledMessageDto(
    int Id,
    string Channel,
    string Audience,
    string? UserId,
    int? GroupId,
    string Subject,
    string Body,
    DateTime SendAtUtc,
    string Status,
    DateTime CreatedAtUtc,
    DateTime? SentAtUtc);
