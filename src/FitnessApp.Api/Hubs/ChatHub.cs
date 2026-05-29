using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FitnessApp.Api.Hubs;

// Real-time delivery channel for chat. Messages are persisted via ChatController (REST);
// this hub only pushes new messages to connected clients addressed by their user id.
[Authorize]
public class ChatHub : Hub
{
}
