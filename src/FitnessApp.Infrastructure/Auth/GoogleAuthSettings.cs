namespace FitnessApp.Infrastructure.Auth;

public class GoogleAuthSettings
{
    /// <summary>
    /// OAuth 2.0 Web client ID from Google Cloud Console
    /// (looks like "xxxxx.apps.googleusercontent.com"). Public by design.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;
}
