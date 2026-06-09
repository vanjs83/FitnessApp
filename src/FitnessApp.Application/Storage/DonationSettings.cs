namespace FitnessApp.Application.Storage;

public class DonationSettings
{
    /// <summary>Public Buy Me a Coffee page the "Doniraj" button links to.</summary>
    public string Url { get; set; } = "https://www.buymeacoffee.com/fitnessapp";

    /// <summary>
    /// Secret from the BMAC webhook configuration, used to verify the HMAC-SHA256
    /// signature on incoming webhook calls. Empty = signature verification skipped (dev only).
    /// </summary>
    public string WebhookSecret { get; set; } = "";
}
