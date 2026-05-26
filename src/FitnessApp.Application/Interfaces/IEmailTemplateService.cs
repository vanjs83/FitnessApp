namespace FitnessApp.Application.Interfaces;

public interface IEmailTemplateService
{
    string Render(string key, string? language, IReadOnlyDictionary<string, string> placeholders);
}
