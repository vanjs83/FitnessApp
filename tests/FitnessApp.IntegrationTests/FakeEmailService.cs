using FitnessApp.Application.Interfaces;

namespace FitnessApp.IntegrationTests;

/// <summary>
/// No-op email service for tests so flows that notify by email (e.g. trainer requests)
/// never touch a real SMTP server. Reports itself as unconfigured, which the handlers
/// treat as "skip sending".
/// </summary>
public class FakeEmailService : IEmailService
{
    public bool IsConfigured => false;

    public Task SendAsync(string toEmail, string subject, string body,
        string? fromEmail = null, string? fromName = null,
        string? replyTo = null, string? replyToName = null) => Task.CompletedTask;

    public Task<(bool ok, string? error)> SendTemplatedAsync(string toEmail, string subject,
        string templateKey, string? language, IReadOnlyDictionary<string, string> placeholders,
        string? replyTo = null, string? replyToName = null) => Task.FromResult((true, (string?)null));
}
