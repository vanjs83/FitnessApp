using System.Net;
using System.Net.Mail;

namespace FitnessApp.Api.Services;

public class SmtpSettings
{
    public string Host { get; set; } = "";
    public int Port { get; set; } = 587;
    public bool UseSsl { get; set; } = true;
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string FromEmail { get; set; } = "";
    public string FromName { get; set; } = "FitnessApp";
}

public class EmailService
{
    private readonly SmtpSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _settings = config.GetSection("Smtp").Get<SmtpSettings>() ?? new SmtpSettings();
        _logger = logger;
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_settings.Host) &&
        !string.IsNullOrWhiteSpace(_settings.Username) &&
        !string.IsNullOrWhiteSpace(_settings.Password) &&
        !string.IsNullOrWhiteSpace(_settings.FromEmail);

    public async Task SendAsync(
        string toEmail,
        string subject,
        string body,
        string? fromEmail = null,
        string? fromName = null,
        string? replyTo = null,
        string? replyToName = null)
    {
        if (!IsConfigured)
            throw new InvalidOperationException("SMTP nije konfiguriran u appsettings.json (Smtp sekcija).");

        var displayFrom = string.IsNullOrWhiteSpace(fromEmail) ? _settings.FromEmail : fromEmail;
        var displayName = string.IsNullOrWhiteSpace(fromName) ? _settings.FromName : fromName;

        using var msg = new MailMessage
        {
            From = new MailAddress(displayFrom, displayName),
            Sender = new MailAddress(_settings.FromEmail, _settings.FromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };
        msg.To.Add(toEmail);
        if (!string.IsNullOrWhiteSpace(replyTo))
            msg.ReplyToList.Add(new MailAddress(replyTo, replyToName ?? replyTo));

        using var client = new SmtpClient(_settings.Host, _settings.Port)
        {
            Credentials = new NetworkCredential(_settings.Username, _settings.Password),
            EnableSsl = _settings.UseSsl
        };

        await client.SendMailAsync(msg);
        _logger.LogInformation("Email poslan na {To} (from: {From}, subject: {Subject})", toEmail, displayFrom, subject);
    }
}
