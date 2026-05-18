using System.ComponentModel.DataAnnotations;

namespace FitnessApp.Application.DTOs.Support;

public class SupportContactRequest
{
    [Required, MaxLength(200)]
    public string Subject { get; set; } = string.Empty;

    [Required, MaxLength(5000)]
    public string Body { get; set; } = string.Empty;

    public string? Language { get; set; }
}

public class SupportStatusDto
{
    public bool Configured { get; set; }
}
