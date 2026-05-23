namespace FitnessApp.Application.Interfaces;

public interface IPushNotificationService
{
    Task SendToUserAsync(string userId, string title, string body, IDictionary<string, string>? data = null, CancellationToken ct = default);
    Task SendToTokenAsync(string token, string title, string body, IDictionary<string, string>? data = null, CancellationToken ct = default);
}
