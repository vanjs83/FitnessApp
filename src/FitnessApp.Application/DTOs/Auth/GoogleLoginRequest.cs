namespace FitnessApp.Application.DTOs.Auth;

public class GoogleLoginRequest
{
    /// <summary>The Google ID token (JWT) returned by Google Identity Services.</summary>
    public string Credential { get; set; } = string.Empty;

    /// <summary>
    /// Desired role for a brand-new account (sent only from the Register tab).
    /// Ignored when the account already exists. Defaults to Client when omitted.
    /// </summary>
    public string? Role { get; set; }
}
