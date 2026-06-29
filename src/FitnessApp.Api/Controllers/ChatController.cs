using FitnessApp.Application.DTOs.Chat;
using FitnessApp.Application.Features.Chat.Commands;
using FitnessApp.Application.Features.Chat.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FitnessApp.Api.Controllers;

[Authorize]
[Produces("application/json")]
[Route("api/v{version:apiVersion}/chat")]
public class ChatController : ApiControllerBase
{
    private readonly ISender _sender;

    public ChatController(ISender sender) => _sender = sender;

    /// <summary>The caller's conversations with unread counts.</summary>
    [HttpGet("conversations")]
    [ResponseCache(CacheProfileName = "Volatile")]
    [ProducesResponseType<IReadOnlyList<ConversationDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ConversationDto>>> GetConversations()
        => Ok(await _sender.Send(new GetConversationsQuery()));

    /// <summary>Messages exchanged with a partner (optionally only those after an id).</summary>
    [HttpGet("with/{partnerId}")]
    [ResponseCache(CacheProfileName = "Volatile")]
    [ProducesResponseType<IReadOnlyList<ChatMessageDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ChatMessageDto>>> GetMessages(string partnerId, [FromQuery] int? afterId)
        => HandleResult(await _sender.Send(new GetMessagesQuery(partnerId, afterId)));

    /// <summary>Send a message to a partner.</summary>
    [HttpPost("with/{partnerId}")]
    [ProducesResponseType<ChatMessageDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChatMessageDto>> Send(string partnerId, SendMessageRequest request)
        => HandleCreated(await _sender.Send(new SendMessageCommand(partnerId, request.Body)));

    /// <summary>Mark all messages from a partner as read.</summary>
    [HttpPost("with/{partnerId}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkRead(string partnerId)
        => HandleResult(await _sender.Send(new MarkMessagesReadCommand(partnerId)));
}
