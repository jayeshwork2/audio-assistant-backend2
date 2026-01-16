using AudioAssistant.Api.Models.DTOs;
using AudioAssistant.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text;

namespace AudioAssistant.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
//[Authorize]
public class ExportController : ControllerBase
{
    private readonly IExportService _exportService;
    private readonly ILogger<ExportController> _logger;

    public ExportController(
        IExportService exportService,
        ILogger<ExportController> logger)
    {
        _exportService = exportService;
        _logger = logger;
    }

    /// <summary>
    /// Export meeting as PDF (HTML format)
    /// </summary>
    [HttpPost("pdf/{meetingId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExportAsPdf(int meetingId, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized(new ErrorResponse { Message = "User ID not found in token" });
        }

        _logger.LogInformation("Exporting meeting {MeetingId} as PDF for user {UserId}", meetingId, userId);

        var result = await _exportService.ExportAsPdfAsync(meetingId, userId.Value, cancellationToken);

        if (!result.Success)
        {
            return BadRequest(new ErrorResponse { Message = result.ErrorMessage ?? "Export failed" });
        }

        // MVP Fix: returning HTML instead of PDF
        var bytes = Encoding.UTF8.GetBytes(result.Content);
        return File(bytes, "text/html", result.FileName + ".html");
    }

    /// <summary>
    /// Export meeting as Markdown
    /// </summary>
    [HttpPost("markdown/{meetingId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExportAsMarkdown(int meetingId, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized(new ErrorResponse { Message = "User ID not found in token" });
        }

        _logger.LogInformation("Exporting meeting {MeetingId} as Markdown for user {UserId}", meetingId, userId);

        var result = await _exportService.ExportAsMarkdownAsync(meetingId, userId.Value, cancellationToken);

        if (!result.Success)
        {
            return BadRequest(new ErrorResponse { Message = result.ErrorMessage ?? "Export failed" });
        }

        var bytes = Encoding.UTF8.GetBytes(result.Content);
        return File(bytes, result.ContentType, result.FileName);
    }

    /// <summary>
    /// Export meeting as plain text
    /// </summary>
    [HttpPost("text/{meetingId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExportAsText(int meetingId, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized(new ErrorResponse { Message = "User ID not found in token" });
        }

        _logger.LogInformation("Exporting meeting {MeetingId} as text for user {UserId}", meetingId, userId);

        var result = await _exportService.ExportAsPlainTextAsync(meetingId, userId.Value, cancellationToken);

        if (!result.Success)
        {
            return BadRequest(new ErrorResponse { Message = result.ErrorMessage ?? "Export failed" });
        }

        var bytes = Encoding.UTF8.GetBytes(result.Content);
        return File(bytes, result.ContentType, result.FileName);
    }

    /// <summary>
    /// Get export history for current user
    /// </summary>
    [HttpGet("history")]
    [ProducesResponseType(typeof(IEnumerable<Models.Export>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHistory(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized(new ErrorResponse { Message = "User ID not found in token" });
        }

        var history = await _exportService.GetExportHistoryAsync(userId.Value, cancellationToken);
        return Ok(history);
    }

    private int? GetUserId()
    {
        // NO-AUTH MODE: Return default user ID 1
        return 1;
        /*
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }
        return null;
        */
    }
}
