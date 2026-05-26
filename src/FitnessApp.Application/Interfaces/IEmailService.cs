namespace FitnessApp.Application.Interfaces;

public interface IEmailService
{
    bool IsConfigured { get; }

    Task SendAsync(
        string toEmail,
        string subject,
        string body,
        string? fromEmail = null,
        string? fromName = null,
        string? replyTo = null,
        string? replyToName = null);

    Task<(bool ok, string? error)> SendTemplatedAsync(
        string toEmail,
        string subject,
        string templateKey,
        string? language,
        IReadOnlyDictionary<string, string> placeholders,
        string? replyTo = null,
        string? replyToName = null);
}
