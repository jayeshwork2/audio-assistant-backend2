using AudioAssistant.Api.Models;
using AudioAssistant.Api.Models.DTOs;

namespace AudioAssistant.Api.Services;

/// <summary>
/// Service for meeting intelligence and analysis
/// </summary>
public interface IMeetingService
{
    /// <summary>
    /// Create a meeting linked to a conversation
    /// </summary>
    Task<Meeting> CreateMeetingAsync(
        string conversationId,
        string title,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyze a meeting transcript to extract context
    /// </summary>
    Task<MeetingDetectionResult> AnalyzeMeetingAsync(
        string transcript,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Detect meeting type from transcript
    /// </summary>
    Task<string> DetectMeetingTypeAsync(
        string transcript,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Detect domain/industry from transcript
    /// </summary>
    Task<string> DetectDomainAsync(
        string transcript,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Detect formality level from transcript
    /// </summary>
    Task<string> DetectFormalityAsync(
        string transcript,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Detect urgency level from transcript
    /// </summary>
    Task<string> DetectUrgencyAsync(
        string transcript,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate comprehensive meeting notes
    /// </summary>
    Task<MeetingNotes> GenerateMeetingNotesAsync(
        int meetingId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Extract action items from transcripts
    /// </summary>
    Task<List<string>> ExtractActionItemsAsync(
        string transcript,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Extract key points from transcripts
    /// </summary>
    Task<List<string>> ExtractKeyPointsAsync(
        string transcript,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get meeting by ID
    /// </summary>
    Task<Meeting?> GetMeetingAsync(
        int meetingId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get meeting summary
    /// </summary>
    Task<string> GetMeetingSummaryAsync(
        int meetingId,
        CancellationToken cancellationToken = default);
}
