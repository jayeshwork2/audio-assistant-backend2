namespace AudioAssistant.Api.Services;

/// <summary>
/// Service for exporting meetings and transcripts
/// </summary>
public interface IExportService
{
    /// <summary>
    /// Export meeting as PDF (HTML format for PDF conversion)
    /// </summary>
    Task<ExportResult> ExportAsPdfAsync(
        int meetingId,
        int userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Export meeting as Markdown
    /// </summary>
    Task<ExportResult> ExportAsMarkdownAsync(
        int meetingId,
        int userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Export meeting as plain text
    /// </summary>
    Task<ExportResult> ExportAsPlainTextAsync(
        int meetingId,
        int userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get export history for a user
    /// </summary>
    Task<IEnumerable<Models.Export>> GetExportHistoryAsync(
        int userId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of an export operation
/// </summary>
public class ExportResult
{
    public string Content { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}
