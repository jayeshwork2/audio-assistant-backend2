using AudioAssistant.Api.Models;
using System.Text;

namespace AudioAssistant.Api.Utilities;

/// <summary>
/// PDF generator using HTML-based approach
/// In production, this would use a library like iTextSharp, SelectPdf, or PuppeteerSharp
/// For now, generates HTML that can be converted to PDF
/// </summary>
public class PdfGenerator
{
    public static string GenerateHtml(Meeting meeting, MeetingNotes? notes, IEnumerable<ConversationExchange>? exchanges)
    {
        var html = new StringBuilder();

        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html>");
        html.AppendLine("<head>");
        html.AppendLine("  <meta charset='UTF-8'>");
        html.AppendLine($"  <title>{meeting.Title}</title>");
        html.AppendLine("  <style>");
        html.AppendLine("    body { font-family: Arial, sans-serif; margin: 40px; color: #333; }");
        html.AppendLine("    h1 { color: #2c3e50; border-bottom: 3px solid #3498db; padding-bottom: 10px; }");
        html.AppendLine("    h2 { color: #34495e; margin-top: 30px; border-bottom: 1px solid #bdc3c7; padding-bottom: 5px; }");
        html.AppendLine("    .metadata { background: #ecf0f1; padding: 15px; border-radius: 5px; margin: 20px 0; }");
        html.AppendLine("    .metadata p { margin: 5px 0; }");
        html.AppendLine("    .label { font-weight: bold; color: #2c3e50; }");
        html.AppendLine("    ul { line-height: 1.8; }");
        html.AppendLine("    .exchange { background: #f8f9fa; padding: 15px; margin: 15px 0; border-left: 4px solid #3498db; }");
        html.AppendLine("    .timestamp { color: #7f8c8d; font-size: 0.9em; }");
        html.AppendLine("    .user-input { color: #2980b9; font-weight: bold; }");
        html.AppendLine("    .ai-response { color: #27ae60; font-weight: bold; }");
        html.AppendLine("    .footer { margin-top: 50px; padding-top: 20px; border-top: 1px solid #bdc3c7; font-size: 0.9em; color: #7f8c8d; }");
        html.AppendLine("  </style>");
        html.AppendLine("</head>");
        html.AppendLine("<body>");

        // Title
        html.AppendLine($"  <h1>{EscapeHtml(meeting.Title)}</h1>");

        // Metadata
        html.AppendLine("  <div class='metadata'>");
        html.AppendLine($"    <p><span class='label'>Date:</span> {meeting.StartTime:yyyy-MM-dd HH:mm:ss}</p>");
        
        if (meeting.EndTime.HasValue)
        {
            html.AppendLine($"    <p><span class='label'>End Time:</span> {meeting.EndTime.Value:yyyy-MM-dd HH:mm:ss}</p>");
            html.AppendLine($"    <p><span class='label'>Duration:</span> {meeting.Duration ?? 0} seconds</p>");
        }
        
        if (!string.IsNullOrEmpty(meeting.Type))
            html.AppendLine($"    <p><span class='label'>Type:</span> {EscapeHtml(meeting.Type)}</p>");
        
        if (!string.IsNullOrEmpty(meeting.Domain))
            html.AppendLine($"    <p><span class='label'>Domain:</span> {EscapeHtml(meeting.Domain)}</p>");
        
        if (!string.IsNullOrEmpty(meeting.Participants))
            html.AppendLine($"    <p><span class='label'>Participants:</span> {EscapeHtml(meeting.Participants)}</p>");
        
        html.AppendLine("  </div>");

        // Meeting Notes
        if (notes != null)
        {
            if (!string.IsNullOrEmpty(notes.Summary))
            {
                html.AppendLine("  <h2>Summary</h2>");
                html.AppendLine($"  <p>{EscapeHtml(notes.Summary).Replace("\n", "<br/>")}</p>");
            }

            if (!string.IsNullOrEmpty(notes.KeyPoints))
            {
                html.AppendLine("  <h2>Key Points</h2>");
                html.AppendLine("  <ul>");
                var keyPoints = notes.KeyPoints.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                foreach (var point in keyPoints)
                {
                    html.AppendLine($"    <li>{EscapeHtml(point)}</li>");
                }
                html.AppendLine("  </ul>");
            }

            if (!string.IsNullOrEmpty(notes.ActionItems))
            {
                html.AppendLine("  <h2>Action Items</h2>");
                html.AppendLine("  <ul>");
                var actionItems = notes.ActionItems.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                foreach (var item in actionItems)
                {
                    html.AppendLine($"    <li>{EscapeHtml(item)}</li>");
                }
                html.AppendLine("  </ul>");
            }

            if (!string.IsNullOrEmpty(notes.Decisions))
            {
                html.AppendLine("  <h2>Decisions</h2>");
                html.AppendLine($"  <p>{EscapeHtml(notes.Decisions).Replace("\n", "<br/>")}</p>");
            }
        }

        // Conversation History
        if (exchanges != null && exchanges.Any())
        {
            html.AppendLine("  <h2>Conversation Transcript</h2>");

            foreach (var exchange in exchanges.OrderBy(e => e.Sequence))
            {
                html.AppendLine("  <div class='exchange'>");
                html.AppendLine($"    <p class='user-input'>User <span class='timestamp'>({exchange.InputTimestamp:HH:mm:ss})</span>:</p>");
                html.AppendLine($"    <p>{EscapeHtml(exchange.UserInput)}</p>");

                if (!string.IsNullOrEmpty(exchange.AiResponse))
                {
                    html.AppendLine($"    <p class='ai-response'>Assistant <span class='timestamp'>({exchange.ResponseTimestamp:HH:mm:ss})</span>:</p>");
                    html.AppendLine($"    <p>{EscapeHtml(exchange.AiResponse)}</p>");
                }
                html.AppendLine("  </div>");
            }
        }

        // Footer
        html.AppendLine("  <div class='footer'>");
        html.AppendLine($"    <p>Generated on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>");
        html.AppendLine("  </div>");

        html.AppendLine("</body>");
        html.AppendLine("</html>");

        return html.ToString();
    }

    private static string EscapeHtml(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#39;");
    }
}
