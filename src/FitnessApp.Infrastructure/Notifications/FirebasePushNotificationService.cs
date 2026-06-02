using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using FitnessApp.Application.Interfaces;
using FitnessApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FitnessApp.Infrastructure.Notifications;

public class FirebasePushNotificationService : IPushNotificationService
{
    private readonly AppDbContext _db;
    private readonly ILogger<FirebasePushNotificationService> _logger;

    public FirebasePushNotificationService(AppDbContext db, ILogger<FirebasePushNotificationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task SendToUserAsync(string userId, string title, string body, IDictionary<string, string>? data = null, CancellationToken ct = default)
    {
        var tokens = await _db.Devices
            .Where(t => t.UserId == userId && t.IsActive)
            .Select(t => t.Token)
            .ToListAsync(ct);

        if (tokens.Count == 0)
        {
            _logger.LogInformation("Korisnik {UserId} nema aktivnih device tokena.", userId);
            return;
        }

        foreach (var token in tokens)
        {
            await SendInternalAsync(token, title, body, data, ct);
        }
    }

    public Task SendToTokenAsync(string token, string title, string body, IDictionary<string, string>? data = null, CancellationToken ct = default)
        => SendInternalAsync(token, title, body, data, ct);

    private async Task SendInternalAsync(string token, string title, string body, IDictionary<string, string>? data, CancellationToken ct)
    {
        // Firebase isn't initialised (missing/invalid credentials file at startup) —
        // skip gracefully instead of throwing on FirebaseMessaging.DefaultInstance.
        if (FirebaseApp.DefaultInstance is null)
        {
            _logger.LogWarning("Firebase nije inicijaliziran (DefaultInstance je null) — push se preskače. Provjeri credentials fajl/putanju.");
            return;
        }

        var message = new Message
        {
            Token = token,
            Notification = new Notification { Title = title, Body = body },
            Data = data is null ? null : new Dictionary<string, string>(data)
        };

        try
        {
            await FirebaseMessaging.DefaultInstance.SendAsync(message, ct);
        }
        catch (FirebaseMessagingException ex) when (
            ex.MessagingErrorCode is MessagingErrorCode.Unregistered or MessagingErrorCode.InvalidArgument)
        {
            _logger.LogWarning("Deaktiviram nevažeći FCM token: {Code}", ex.MessagingErrorCode);
            var entity = await _db.Devices.FirstOrDefaultAsync(t => t.Token == token, ct);
            if (entity is not null)
            {
                entity.IsActive = false;
                await _db.SaveChangesAsync(ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Slanje push notifikacije nije uspjelo.");
        }
    }
}
