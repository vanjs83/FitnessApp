namespace FitnessApp.Application.DTOs.Email;

public class SendEmailToClientRequest
{
    public string ClientId { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? Language { get; set; }
}

public class EmailStatusDto
{
    public bool Configured { get; set; }
}

public class NotifyPlanReadyRequest
{
    public string ClientId { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public string PlanType { get; set; } = "training"; // training | nutrition
    public string? Language { get; set; }
}

public class SendEmailToTrainersRequest
{
    public List<string> TrainerIds { get; set; } = new();
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? Language { get; set; } // "hr" | "en", default "hr"
}

public class EmailSendResultDto
{
    public List<string> Sent { get; set; } = new();
    public List<EmailFailureDto> Failed { get; set; } = new();
}

public class EmailFailureDto
{
    public string TrainerId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
}

// Generic admin → users messaging (email or push) to any mix of trainers/clients.
public class SendMessageToUsersRequest
{
    public List<string> UserIds { get; set; } = new();
    public string Subject { get; set; } = string.Empty; // email subject / push title
    public string Body { get; set; } = string.Empty;
    public string? Language { get; set; }

    /// <summary>When to deliver. Null (or omitted) sends right away; a future value schedules it.</summary>
    public DateTime? SendAtUtc { get; set; }
}

public class MessageSendResultDto
{
    public List<string> Sent { get; set; } = new();
    public List<MessageFailureDto> Failed { get; set; } = new();
}

public class MessageFailureDto
{
    public string UserId { get; set; } = string.Empty;
    public string Recipient { get; set; } = string.Empty; // email or name
    public string Error { get; set; } = string.Empty;
}
