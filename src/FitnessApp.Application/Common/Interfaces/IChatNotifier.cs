using FitnessApp.Application.DTOs.Chat;

namespace FitnessApp.Application.Common.Interfaces;

/// <summary>
/// Pushes a chat message to connected clients in real time, without the Application
/// layer depending on SignalR. Implemented in the API layer over the ChatHub.
/// </summary>
public interface IChatNotifier
{
    Task NotifyMessageAsync(IEnumerable<string> userIds, ChatMessageDto message, CancellationToken cancellationToken = default);
}
