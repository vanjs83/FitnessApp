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
    private readonly EmailTemplateService _templates;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, EmailTemplateService templates, ILogger<EmailService> logger)
    {
        _settings = config.GetSection("Smtp").Get<SmtpSettings>() ?? new SmtpSettings();
        _templates = templates;
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
        {
            _logger.LogWarning("SMTP send aborted — not configured. To={To}, Subject={Subject}", toEmail, subject);
            throw new InvalidOperationException("SMTP is not configured in appsettings.json (Smtp section).");
        }

        var displayFrom = string.IsNullOrWhiteSpace(fromEmail) ? _settings.FromEmail : fromEmail;
        var displayName = string.IsNullOrWhiteSpace(fromName) ? _settings.FromName : fromName;

        _logger.LogInformation(
            "SMTP send attempt → Host={Host}:{Port} SSL={Ssl} User={User} From={From} To={To} ReplyTo={ReplyTo} Subject={Subject}",
            _settings.Host, _settings.Port, _settings.UseSsl, _settings.Username, displayFrom, toEmail, replyTo ?? "(none)", subject);

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
            EnableSsl = _settings.UseSsl,
            Timeout = 20000
        };

        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            await client.SendMailAsync(msg);
            sw.Stop();
            _logger.LogInformation("SMTP send OK → To={To} Subject={Subject} ElapsedMs={Elapsed}",
                toEmail, subject, sw.ElapsedMilliseconds);
        }
        catch (SmtpException smtpEx)
        {
            sw.Stop();
            _logger.LogError(smtpEx,
                "SMTP send FAILED (SmtpException) → To={To} StatusCode={Status} Host={Host}:{Port} SSL={Ssl} User={User} ElapsedMs={Elapsed} InnerMessage={Inner}",
                toEmail, smtpEx.StatusCode, _settings.Host, _settings.Port, _settings.UseSsl, _settings.Username,
                sw.ElapsedMilliseconds, smtpEx.InnerException?.Message ?? "(none)");
            throw;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex,
                "SMTP send FAILED ({ExType}) → To={To} Host={Host}:{Port} SSL={Ssl} User={User} ElapsedMs={Elapsed} InnerMessage={Inner}",
                ex.GetType().Name, toEmail, _settings.Host, _settings.Port, _settings.UseSsl, _settings.Username,
                sw.ElapsedMilliseconds, ex.InnerException?.Message ?? "(none)");
            throw;
        }
    }

    public async Task<(bool ok, string? error)> SendTemplatedAsync(
        string toEmail,
        string subject,
        string templateKey,
        string? language,
        IReadOnlyDictionary<string, string> placeholders,
        string? replyTo = null,
        string? replyToName = null)
    {
        if (!IsConfigured)
            return (false, "SMTP nije konfiguriran u appsettings.json (Smtp sekcija).");

        string html;
        try
        {
            html = _templates.Render(templateKey, language, placeholders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Render template failed: {Template} (lang={Lang})", templateKey, language);
            return (false, "Neuspjeh pripreme poruke iz predloška.");
        }

        try
        {
            await SendAsync(toEmail, subject, html, replyTo: replyTo, replyToName: replyToName);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SendTemplated failed: {Template} → {To}", templateKey, toEmail);
            return (false, ex.Message);
        }
    }
}
