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
}
