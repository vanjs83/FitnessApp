using FitnessApp.Api.Hubs;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.DTOs.Chat;
using Microsoft.AspNetCore.SignalR;

namespace FitnessApp.Api.Services;

/// <summary>SignalR-backed implementation of <see cref="IChatNotifier"/>.</summary>
public class ChatNotifier : IChatNotifier
{
    private readonly IHubContext<ChatHub> _hub;

    public ChatNotifier(IHubContext<ChatHub> hub) => _hub = hub;

    public Task NotifyMessageAsync(IEnumerable<string> userIds, ChatMessageDto message, CancellationToken cancellationToken = default)
        => _hub.Clients.Users(userIds.ToList()).SendAsync("ReceiveMessage", message, cancellationToken);
}
