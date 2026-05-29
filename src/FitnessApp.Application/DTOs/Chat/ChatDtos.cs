namespace FitnessApp.Application.DTOs.Chat;

public class ChatMessageDto
{
    public int Id { get; set; }
    public string SenderId { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
    public DateTime? ReadAt { get; set; }
}

public class SendMessageRequest
{
    public string Body { get; set; } = string.Empty;
}

public class ConversationDto
{
    public string PartnerId { get; set; } = string.Empty;
    public string? PartnerName { get; set; }
    public string? PartnerImageUrl { get; set; }
    public string? LastMessage { get; set; }
    public DateTime? LastMessageAt { get; set; }
    public int UnreadCount { get; set; }
}
