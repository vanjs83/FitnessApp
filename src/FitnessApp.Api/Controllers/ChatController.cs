using FitnessApp.Application.DTOs.Chat;
using FitnessApp.Application.Features.Chat.Commands;
using FitnessApp.Application.Features.Chat.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitnessApp.Api.Controllers;

[Authorize]
[Route("api/chat")]
public class ChatController : ApiControllerBase
{
    private readonly ISender _sender;

    public ChatController(ISender sender) => _sender = sender;

    [HttpGet("conversations")]
    [ResponseCache(CacheProfileName = "Volatile")]
    public async Task<ActionResult<IReadOnlyList<ConversationDto>>> GetConversations()
        => Ok(await _sender.Send(new GetConversationsQuery()));

    [HttpGet("with/{partnerId}")]
    [ResponseCache(CacheProfileName = "Volatile")]
    public async Task<ActionResult<IReadOnlyList<ChatMessageDto>>> GetMessages(string partnerId, [FromQuery] int? afterId)
        => HandleResult(await _sender.Send(new GetMessagesQuery(partnerId, afterId)));

    [HttpPost("with/{partnerId}")]
    public async Task<ActionResult<ChatMessageDto>> Send(string partnerId, SendMessageRequest request)
        => HandleResult(await _sender.Send(new SendMessageCommand(partnerId, request.Body)));

    [HttpPost("with/{partnerId}/read")]
    public async Task<IActionResult> MarkRead(string partnerId)
        => HandleResult(await _sender.Send(new MarkMessagesReadCommand(partnerId)));
}
