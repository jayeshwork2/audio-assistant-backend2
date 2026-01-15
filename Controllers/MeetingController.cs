using AudioAssistant.Api.Models.DTOs;
using AudioAssistant.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AudioAssistant.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MeetingController : ControllerBase
{
    private readonly IMeetingService _meetingService;
    private readonly ILogger<MeetingController> _logger;

    public MeetingController(
        IMeetingService meetingService,
        ILogger<MeetingController> logger)
    {
        _meetingService = meetingService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new meeting
    /// </summary>
    [HttpPost("create")]
    [ProducesResponseType(typeof(Models.Meeting), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateMeeting([FromBody] MeetingRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating meeting for conversation {ConversationId}", request.ConversationId);

        try
        {
            var meeting = await _meetingService.CreateMeetingAsync(
                request.ConversationId,
                request.Title,
                cancellationToken);

            return Ok(meeting);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ErrorResponse { Message = ex.Message });
        }
    }

    /// <summary>
    /// Detect meeting type from transcript
    /// </summary>
    [HttpPost("detect-type")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> DetectMeetingType([FromBody] MeetingDetectRequest request, CancellationToken cancellationToken)
    {
        var meetingType = await _meetingService.DetectMeetingTypeAsync(request.Transcript, cancellationToken);
        return Ok(new { meetingType });
    }

    /// <summary>
    /// Detect domain from transcript
    /// </summary>
    [HttpPost("detect-domain")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> DetectDomain([FromBody] MeetingDetectRequest request, CancellationToken cancellationToken)
    {
        var domain = await _meetingService.DetectDomainAsync(request.Transcript, cancellationToken);
        return Ok(new { domain });
    }

    /// <summary>
    /// Detect formality level from transcript
    /// </summary>
    [HttpPost("detect-formality")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> DetectFormality([FromBody] MeetingDetectRequest request, CancellationToken cancellationToken)
    {
        var formality = await _meetingService.DetectFormalityAsync(request.Transcript, cancellationToken);
        return Ok(new { formality });
    }

    /// <summary>
    /// Detect urgency level from transcript
    /// </summary>
    [HttpPost("detect-urgency")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> DetectUrgency([FromBody] MeetingDetectRequest request, CancellationToken cancellationToken)
    {
        var urgency = await _meetingService.DetectUrgencyAsync(request.Transcript, cancellationToken);
        return Ok(new { urgency });
    }

    /// <summary>
    /// Generate meeting notes
    /// </summary>
    [HttpPost("{id}/notes/generate")]
    [ProducesResponseType(typeof(Models.MeetingNotes), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GenerateNotes(int id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Generating notes for meeting {MeetingId}", id);

        try
        {
            var notes = await _meetingService.GenerateMeetingNotesAsync(id, cancellationToken);
            return Ok(notes);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ErrorResponse { Message = ex.Message });
        }
    }

    /// <summary>
    /// Get meeting details
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Models.Meeting), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMeeting(int id, CancellationToken cancellationToken)
    {
        var meeting = await _meetingService.GetMeetingAsync(id, cancellationToken);

        if (meeting == null)
        {
            return NotFound(new ErrorResponse { Message = "Meeting not found" });
        }

        return Ok(meeting);
    }

    /// <summary>
    /// Get meeting summary
    /// </summary>
    [HttpGet("{id}/summary")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSummary(int id, CancellationToken cancellationToken)
    {
        var summary = await _meetingService.GetMeetingSummaryAsync(id, cancellationToken);
        return Ok(new { summary });
    }

    /// <summary>
    /// Get action items from transcript
    /// </summary>
    [HttpPost("action-items")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActionItems([FromBody] MeetingDetectRequest request, CancellationToken cancellationToken)
    {
        var actionItems = await _meetingService.ExtractActionItemsAsync(request.Transcript, cancellationToken);
        return Ok(new { actionItems });
    }

    /// <summary>
    /// Get key points from transcript
    /// </summary>
    [HttpPost("key-points")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetKeyPoints([FromBody] MeetingDetectRequest request, CancellationToken cancellationToken)
    {
        var keyPoints = await _meetingService.ExtractKeyPointsAsync(request.Transcript, cancellationToken);
        return Ok(new { keyPoints });
    }
}
