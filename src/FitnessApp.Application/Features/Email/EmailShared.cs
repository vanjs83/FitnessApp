using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;

namespace FitnessApp.Application.Features.Email;

public record EmailSendStatusDto(bool Sent, string? To, string? From);

public record EmailParties(UserInfo Trainer, UserInfo Client);

internal static class EmailRecipients
{
    /// <summary>
    /// Validates that the current trainer may email the given client and that both have an email address.
    /// </summary>
    public static async Task<Result<EmailParties>> ResolveAsync(
        IUserDirectory users, string currentUserId, string clientId, CancellationToken ct)
    {
        var client = await users.FindAsync(clientId, ct);
        if (client == null)
            return Result<EmailParties>.NotFound("Client not found.");
        if (client.TrainerId != currentUserId)
            return Result<EmailParties>.Forbidden();
        if (string.IsNullOrWhiteSpace(client.Email))
            return Result<EmailParties>.Fail(ResultError.Validation, "Client has no email address.");

        var trainer = await users.FindAsync(currentUserId, ct);
        if (trainer == null || string.IsNullOrWhiteSpace(trainer.Email))
            return Result<EmailParties>.Fail(ResultError.Validation, "Your trainer profile has no email.");

        return Result<EmailParties>.Success(new EmailParties(trainer, client));
    }
}
