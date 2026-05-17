using System.ComponentModel.DataAnnotations;

namespace FitnessApp.Application.DTOs.Auth;

public class RegisterRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(6)]
    public string Password { get; set; } = string.Empty;

    public string? FullName { get; set; }

    [Required]
    public string Role { get; set; } = string.Empty;

    public string? TrainerId { get; set; }
}
