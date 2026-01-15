using AudioAssistant.Api.Models.DTOs;
using AudioAssistant.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AudioAssistant.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ResponseController : ControllerBase
{
    private readonly IResponseService _responseService;
    private readonly ILogger<ResponseController> _logger;

    public ResponseController(
        IResponseService responseService,
        ILogger<ResponseController> logger)
    {
        _responseService = responseService;
        _logger = logger;
    }

    /// <summary>
    /// Generate an AI response to a transcript
    /// </summary>
    [HttpPost("generate")]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateResponse([FromBody] ResponseRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized(new ErrorResponse { Message = "User ID not found in token" });
        }

        _logger.LogInformation("Generating AI response for user {UserId}", userId);

        var result = await _responseService.GenerateResponseAsync(
            request.Transcript,
            userId.Value,
            request.ConversationId,
            request.ResponseStyle,
            request.AiProvider,
            cancellationToken);

        if (!result.Success)
        {
            return BadRequest(new ErrorResponse { Message = result.ErrorMessage ?? "Failed to generate response" });
        }

        return Ok(result);
    }

    /// <summary>
    /// Get available response styles
    /// </summary>
    [HttpGet("styles")]
    [ProducesResponseType(typeof(IEnumerable<ResponseStyleInfo>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStyles()
    {
        var styles = await _responseService.GetAvailableStylesAsync();
        return Ok(styles);
    }

    /// <summary>
    /// Get available AI providers for the current user
    /// </summary>
    [HttpGet("providers")]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProviders()
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized(new ErrorResponse { Message = "User ID not found in token" });
        }

        var providers = await _responseService.GetAvailableProvidersAsync(userId.Value);
        return Ok(providers);
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
