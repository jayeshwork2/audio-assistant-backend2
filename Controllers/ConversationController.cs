using AudioAssistant.Api.Models.DTOs;
using AudioAssistant.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AudioAssistant.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ConversationController : ControllerBase
{
    private readonly IConversationService _conversationService;
    private readonly ILogger<ConversationController> _logger;

    public ConversationController(
        IConversationService conversationService,
        ILogger<ConversationController> logger)
    {
        _conversationService = conversationService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new conversation
    /// </summary>
    [HttpPost("create")]
    [ProducesResponseType(typeof(Models.Conversation), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateConversation([FromBody] ConversationRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized(new ErrorResponse { Message = "User ID not found in token" });
        }

        _logger.LogInformation("Creating conversation for user {UserId}", userId);

        var conversation = await _conversationService.CreateConversationAsync(
            userId.Value,
            request.MeetingType,
            request.Domain,
            cancellationToken);

        return Ok(conversation);
    }

    /// <summary>
    /// Add an exchange to a conversation
    /// </summary>
    [HttpPost("{id}/exchange")]
    [ProducesResponseType(typeof(Models.ConversationExchange), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddExchange(int id, [FromBody] ExchangeRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Adding exchange to conversation {ConversationId}", id);

        try
        {
            var exchange = await _conversationService.AddExchangeAsync(
                id,
                request.UserInput,
                request.AiResponse,
                request.ResponseStyle,
                request.AiProvider,
                request.Context,
                cancellationToken);

            return Ok(exchange);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ErrorResponse { Message = ex.Message });
        }
    }

    /// <summary>
    /// Get conversation details
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Models.Conversation), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetConversation(int id, CancellationToken cancellationToken)
    {
        var conversation = await _conversationService.GetConversationAsync(id, cancellationToken);

        if (conversation == null)
        {
            return NotFound(new ErrorResponse { Message = "Conversation not found" });
        }

        return Ok(conversation);
    }

    /// <summary>
    /// Get conversation history
    /// </summary>
    [HttpGet("{id}/history")]
    [ProducesResponseType(typeof(IEnumerable<Models.ConversationExchange>), StatusCodes.Status200OK)]
    //public async Task<IActionResult> GetHistory(int id, [FromQuery] int limit = 25, CancellationToken cancellationToken)
    public async Task<IActionResult> GetHistory(int id, CancellationToken cancellationToken)
    {
        int limit = 25;
        var history = await _conversationService.GetConversationHistoryAsync(id, limit, cancellationToken);
        return Ok(history);
    }

    /// <summary>
    /// Get aggregated context for a conversation
    /// </summary>
    [HttpGet("{id}/context")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetContext(int id, CancellationToken cancellationToken)
    {
        var context = await _conversationService.GetAggregatedContextAsync(id, cancellationToken);
        return Ok(new { context });
    }

    /// <summary>
    /// Update conversation metadata
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateConversation(int id, [FromBody] UpdateConversationRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await _conversationService.UpdateContextAsync(
                id,
                request.MeetingType,
                request.Domain,
                request.Summary,
                cancellationToken);

            return Ok(new { message = "Conversation updated successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ErrorResponse { Message = ex.Message });
        }
    }

    /// <summary>
    /// End a conversation
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EndConversation(int id, CancellationToken cancellationToken)
    {
        try
        {
            await _conversationService.EndConversationAsync(id, cancellationToken);
            return Ok(new { message = "Conversation ended successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ErrorResponse { Message = ex.Message });
        }
    }

    private int? GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }
        return null;
    }
}
