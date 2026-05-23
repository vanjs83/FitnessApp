namespace FitnessApp.Application.DTOs.Notifications;

public class RegisterDeviceRequest
{
    public string Token { get; set; } = string.Empty;
    public string Platform { get; set; } = "web";
}

public class NotifyClientPlanRequest
{
    public string ClientId { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public string PlanType { get; set; } = "training";
    public string? Language { get; set; }
}
