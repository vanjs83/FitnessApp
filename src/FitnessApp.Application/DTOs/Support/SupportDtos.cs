namespace FitnessApp.Application.DTOs.Support;

public class SupportContactRequest
{
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? Language { get; set; }
}

public class SupportStatusDto
{
    public bool Configured { get; set; }
}
