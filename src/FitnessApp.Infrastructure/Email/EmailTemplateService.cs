using FitnessApp.Application.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FitnessApp.Infrastructure.Email;

public class EmailTemplateService : IEmailTemplateService
{
    private readonly string _templatesRoot;
    private readonly ILogger<EmailTemplateService> _logger;
    private static readonly HashSet<string> SupportedLanguages = new(StringComparer.OrdinalIgnoreCase) { "hr", "en" };
    private const string DefaultLanguage = "hr";

    public EmailTemplateService(IHostEnvironment env, ILogger<EmailTemplateService> logger)
    {
        _templatesRoot = Path.Combine(env.ContentRootPath, "EmailTemplates");
        _logger = logger;
    }

    public string Render(string key, string? language, IReadOnlyDictionary<string, string> placeholders)
    {
        var lang = NormalizeLanguage(language);
        var path = Path.Combine(_templatesRoot, $"{key}.{lang}.html");

        if (!File.Exists(path) && lang != DefaultLanguage)
        {
            _logger.LogWarning("Email template {Key}.{Lang} not found, falling back to {Default}.", key, lang, DefaultLanguage);
            path = Path.Combine(_templatesRoot, $"{key}.{DefaultLanguage}.html");
        }

        if (!File.Exists(path))
            throw new FileNotFoundException($"Email template not found: {key}");

        var html = File.ReadAllText(path);
        foreach (var (k, v) in placeholders)
            html = html.Replace("{{" + k + "}}", System.Net.WebUtility.HtmlEncode(v ?? string.Empty));

        return html;
    }

    private static string NormalizeLanguage(string? lang)
    {
        if (string.IsNullOrWhiteSpace(lang)) return DefaultLanguage;
        var l = lang.Trim().ToLowerInvariant();
        if (l.Length > 2) l = l[..2];
        return SupportedLanguages.Contains(l) ? l : DefaultLanguage;
    }
}
