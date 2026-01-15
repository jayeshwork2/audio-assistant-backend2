using AudioAssistant.Api.Models.DTOs;
using AudioAssistant.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using AudioAssistant.Api.Models;

namespace AudioAssistant.Api.Controllers;

/// <summary>
/// Controller for audio transcription endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
//[Authorize]
public class TranscriptionController : ControllerBase
{
    private readonly ITranscriptionService _transcriptionService;
    private readonly ILogger<TranscriptionController> _logger;

    public TranscriptionController(
        ITranscriptionService transcriptionService,
        ILogger<TranscriptionController> logger)
    {
        _transcriptionService = transcriptionService;
        _logger = logger;
    }

    /// <summary>
    /// Transcribes audio data to text
    /// </summary>
    /// <param name="request">Transcription request with audio data</param>
    /// <returns>Transcription result</returns>
    [HttpPost]
    [ProducesResponseType(typeof(TranscriptionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Models.DTOs.ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Models.DTOs.ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Models.DTOs.ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Transcribe([FromBody] TranscriptionRequest request)
    {
        try
        {
            if (request.AudioData == null || request.AudioData.Length == 0)
            {
                return BadRequest(new Models.DTOs.ErrorResponse
                {
                    Error = "No audio data provided"
                });
            }

            // Validate audio size (max 25MB)
            if (request.AudioData.Length > 25 * 1024 * 1024)
            {
                return BadRequest(new Models.DTOs.ErrorResponse
                {
                    Error = "Audio file too large. Maximum size is 25MB."
                });
            }

            var userId = GetUserId();
            var language = request.Language ?? "en";

            _logger.LogInformation(
                "Transcription request from user {UserId}, language: {Language}, audioSize: {Size} bytes",
                userId, language, request.AudioData.Length);

            var result = await _transcriptionService.TranscribeAsync(
                request.AudioData,
                userId,
                language,
                request.Provider);

            var response = new TranscriptionResponse
            {
                Id = result.Id,
                Text = result.Text,
                Language = result.Language,
                Confidence = result.Confidence,
                Duration = result.Duration,
                Provider = result.Provider,
                Tokens = result.Tokens,
                Timestamp = result.Timestamp,
                UsedFallback = result.Provider != request.Provider && request.Provider != null
            };

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid transcription request");
            return BadRequest(new Models.DTOs.ErrorResponse
            {
                Error = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Transcription operation failed");
            return BadRequest(new Models.DTOs.ErrorResponse
            {
                Error = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during transcription");
            return StatusCode(500, new Models.DTOs.ErrorResponse
            {
                Error = "An unexpected error occurred during transcription"
            });
        }
    }

    /// <summary>
    /// Gets available transcription providers
    /// </summary>
    /// <returns>List of available provider names</returns>
    [HttpGet("providers")]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Models.DTOs.ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAvailableProviders()
    {
        try
        {
            var providers = await _transcriptionService.GetAvailableProvidersAsync();
            return Ok(providers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available providers");
            return StatusCode(500, new Models.DTOs.ErrorResponse
            {
                Error = "Failed to retrieve available providers"
            });
        }
    }

    /// <summary>
    /// Sets the user's preferred transcription provider
    /// </summary>
    /// <param name="provider">Provider name</param>
    /// <returns>Success status</returns>
    [HttpPost("preferences/provider")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Models.DTOs.ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Models.DTOs.ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SetPreferredProvider([FromBody] SetProviderRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Provider))
            {
                return BadRequest(new Models.DTOs.ErrorResponse
                {
                    Error = "Provider name is required"
                });
            }

            var userId = GetUserId();
            await _transcriptionService.SetPreferredProviderAsync(userId, request.Provider);

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting preferred provider");
            return StatusCode(500, new Models.DTOs.ErrorResponse
            {
                Error = "Failed to set preferred provider"
            });
        }
    }

    private int GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user token");
        }
        return userId;
    }
}
