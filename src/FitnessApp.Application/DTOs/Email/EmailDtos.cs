using System.ComponentModel.DataAnnotations;

namespace FitnessApp.Application.DTOs.Email;

public class SendEmailToClientRequest
{
    [Required]
    public string ClientId { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Subject { get; set; } = string.Empty;

    [Required, MaxLength(10000)]
    public string Body { get; set; } = string.Empty;

    public string? Language { get; set; }
}

public class EmailStatusDto
{
    public bool Configured { get; set; }
}

public class NotifyPlanReadyRequest
{
    [Required]
    public string ClientId { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string PlanName { get; set; } = string.Empty;

    [Required]
    public string PlanType { get; set; } = "training"; // training | nutrition

    public string? Language { get; set; }
}

public class SendEmailToTrainersRequest
{
    [Required, MinLength(1)]
    public List<string> TrainerIds { get; set; } = new();

    [Required, MaxLength(200)]
    public string Subject { get; set; } = string.Empty;

    [Required, MaxLength(10000)]
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
