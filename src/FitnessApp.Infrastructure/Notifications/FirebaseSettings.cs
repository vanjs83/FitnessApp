namespace FitnessApp.Infrastructure.Notifications;

public class FirebaseSettings
{
    /// <summary>
    /// Path to the service-account JSON. Absolute = used as-is; relative = resolved
    /// against the app folder (AppContext.BaseDirectory), exactly as written.
    /// </summary>
    public string? CredentialsPath { get; set; }

    /// <summary>
    /// Full service-account JSON content (alternative to a file). Takes precedence
    /// over <see cref="CredentialsPath"/>. Ideal for prod: set as an env var/secret
    /// (Firebase__CredentialsJson) so no file is needed on the host.
    /// </summary>
    public string? CredentialsJson { get; set; }

    public string? ProjectId { get; set; }
}
