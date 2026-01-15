using AudioAssistant.Api.Models.DTOs;
using AudioAssistant.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AudioAssistant.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TranslationController : ControllerBase
{
    private readonly ITranslationService _translationService;
    private readonly ILogger<TranslationController> _logger;

    public TranslationController(
        ITranslationService translationService,
        ILogger<TranslationController> logger)
    {
        _translationService = translationService;
        _logger = logger;
    }

    /// <summary>
    /// Translate text to target language
    /// </summary>
    [HttpPost("translate")]
    [ProducesResponseType(typeof(TranslationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Translate([FromBody] TranslationRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized(new ErrorResponse { Message = "User ID not found in token" });
        }

        _logger.LogInformation("Translating text for user {UserId} to {TargetLanguage}", userId, request.TargetLanguage);

        var result = await _translationService.TranslateAsync(
            request.Text,
            request.TargetLanguage,
            userId.Value,
            request.SourceLanguage,
            cancellationToken);

        if (!result.Success)
        {
            return BadRequest(new ErrorResponse { Message = result.ErrorMessage ?? "Translation failed" });
        }

        return Ok(result);
    }

    /// <summary>
    /// Detect the language of provided text
    /// </summary>
    [HttpPost("detect")]
    [ProducesResponseType(typeof(LanguageDetectionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DetectLanguage([FromBody] DetectLanguageRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Detecting language for text");

        var result = await _translationService.DetectLanguageAsync(request.Text, cancellationToken);

        if (!result.Success)
        {
            return BadRequest(new ErrorResponse { Message = result.ErrorMessage ?? "Language detection failed" });
        }

        return Ok(result);
    }

    /// <summary>
    /// Get list of supported languages
    /// </summary>
    [HttpGet("languages")]
    [ProducesResponseType(typeof(IEnumerable<LanguageInfo>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLanguages()
    {
        var languages = await _translationService.GetSupportedLanguagesAsync();
        return Ok(languages);
    }

    /// <summary>
    /// Get translation history for a transcript
    /// </summary>
    [HttpGet("history/{transcriptId}")]
    [ProducesResponseType(typeof(IEnumerable<Models.Translation>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHistory(int transcriptId, CancellationToken cancellationToken)
    {
        var history = await _translationService.GetTranslationHistoryAsync(transcriptId, cancellationToken);
        return Ok(history);
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
