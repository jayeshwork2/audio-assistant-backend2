using AudioAssistant.Api.Data;
using AudioAssistant.Api.Models;
using AudioAssistant.Api.Utilities;
using Microsoft.EntityFrameworkCore;

namespace AudioAssistant.Api.Services;

public class ExportService : IExportService
{
    private readonly AudioAssistantDbContext _context;
    private readonly ILogger<ExportService> _logger;

    public ExportService(
        AudioAssistantDbContext context,
        ILogger<ExportService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ExportResult> ExportAsPdfAsync(
        int meetingId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var meetingData = await GetMeetingDataAsync(meetingId, cancellationToken);
            if (meetingData == null)
            {
                return new ExportResult
                {
                    Success = false,
                    ErrorMessage = "Meeting not found"
                };
            }

            var (meeting, notes, exchanges) = meetingData.Value;

            // Generate HTML (which can be converted to PDF by a client-side library)
            var htmlContent = PdfGenerator.GenerateHtml(meeting, notes, exchanges);

            var fileName = GenerateFileName(meeting.Title, "html"); // HTML that can be printed as PDF

            await LogExportAsync(userId, meetingId, "pdf", fileName, cancellationToken);

            return new ExportResult
            {
                Content = htmlContent,
                FileName = fileName,
                Format = "html",
                ContentType = "text/html",
                Success = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting meeting {MeetingId} as PDF", meetingId);
            return new ExportResult
            {
                Success = false,
                ErrorMessage = $"Export failed: {ex.Message}"
            };
        }
    }

    public async Task<ExportResult> ExportAsMarkdownAsync(
        int meetingId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var meetingData = await GetMeetingDataAsync(meetingId, cancellationToken);
            if (meetingData == null)
            {
                return new ExportResult
                {
                    Success = false,
                    ErrorMessage = "Meeting not found"
                };
            }

            var (meeting, notes, exchanges) = meetingData.Value;

            var markdownContent = MarkdownFormatter.FormatMeeting(meeting, notes, exchanges);

            var fileName = GenerateFileName(meeting.Title, "md");

            await LogExportAsync(userId, meetingId, "markdown", fileName, cancellationToken);

            return new ExportResult
            {
                Content = markdownContent,
                FileName = fileName,
                Format = "markdown",
                ContentType = "text/markdown",
                Success = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting meeting {MeetingId} as Markdown", meetingId);
            return new ExportResult
            {
                Success = false,
                ErrorMessage = $"Export failed: {ex.Message}"
            };
        }
    }

    public async Task<ExportResult> ExportAsPlainTextAsync(
        int meetingId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var meetingData = await GetMeetingDataAsync(meetingId, cancellationToken);
            if (meetingData == null)
            {
                return new ExportResult
                {
                    Success = false,
                    ErrorMessage = "Meeting not found"
                };
            }

            var (meeting, notes, exchanges) = meetingData.Value;

            var textContent = MarkdownFormatter.FormatPlainText(meeting, notes, exchanges);

            var fileName = GenerateFileName(meeting.Title, "txt");

            await LogExportAsync(userId, meetingId, "text", fileName, cancellationToken);

            return new ExportResult
            {
                Content = textContent,
                FileName = fileName,
                Format = "text",
                ContentType = "text/plain",
                Success = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting meeting {MeetingId} as plain text", meetingId);
            return new ExportResult
            {
                Success = false,
                ErrorMessage = $"Export failed: {ex.Message}"
            };
        }
    }

    public async Task<IEnumerable<Export>> GetExportHistoryAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Exports
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.ExportedAt)
            .Take(50)
            .ToListAsync(cancellationToken);
    }

    private async Task<(Meeting meeting, MeetingNotes? notes, List<ConversationExchange> exchanges)?> GetMeetingDataAsync(
        int meetingId,
        CancellationToken cancellationToken)
    {
        var meeting = await _context.Meetings
            .Include(m => m.Notes)
            .Include(m => m.Conversation)
                .ThenInclude(c => c.Exchanges)
            .FirstOrDefaultAsync(m => m.Id == meetingId, cancellationToken);

        if (meeting == null)
            return null;

        var exchanges = meeting.Conversation.Exchanges.OrderBy(e => e.Sequence).ToList();

        return (meeting, meeting.Notes, exchanges);
    }

    private string GenerateFileName(string meetingTitle, string extension)
    {
        // Sanitize title for filename
        var sanitized = string.Join("_", meetingTitle.Split(Path.GetInvalidFileNameChars()));
        
        if (sanitized.Length > 50)
            sanitized = sanitized.Substring(0, 50);

        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        return $"{sanitized}_{timestamp}.{extension}";
    }

    private async Task LogExportAsync(
        int userId,
        int meetingId,
        string format,
        string fileName,
        CancellationToken cancellationToken)
    {
        var export = new Export
        {
            UserId = userId,
            MeetingId = meetingId,
            Format = format,
            FileName = fileName,
            ExportedAt = DateTime.UtcNow
        };

        _context.Exports.Add(export);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Logged export: User {UserId}, Meeting {MeetingId}, Format {Format}", userId, meetingId, format);
    }
}
