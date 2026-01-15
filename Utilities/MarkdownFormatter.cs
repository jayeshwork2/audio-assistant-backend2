using AudioAssistant.Api.Models;
using System.Text;

namespace AudioAssistant.Api.Utilities;

public class MarkdownFormatter
{
    public static string FormatMeeting(Meeting meeting, MeetingNotes? notes, IEnumerable<ConversationExchange>? exchanges)
    {
        var markdown = new StringBuilder();

        // Title
        markdown.AppendLine($"# {meeting.Title}");
        markdown.AppendLine();

        // Metadata
        markdown.AppendLine("## Meeting Information");
        markdown.AppendLine();
        markdown.AppendLine($"- **Date:** {meeting.StartTime:yyyy-MM-dd HH:mm:ss}");
        
        if (meeting.EndTime.HasValue)
        {
            markdown.AppendLine($"- **End Time:** {meeting.EndTime.Value:yyyy-MM-dd HH:mm:ss}");
            markdown.AppendLine($"- **Duration:** {meeting.Duration ?? 0} seconds");
        }
        
        if (!string.IsNullOrEmpty(meeting.Type))
            markdown.AppendLine($"- **Type:** {meeting.Type}");
        
        if (!string.IsNullOrEmpty(meeting.Domain))
            markdown.AppendLine($"- **Domain:** {meeting.Domain}");
        
        if (!string.IsNullOrEmpty(meeting.Participants))
            markdown.AppendLine($"- **Participants:** {meeting.Participants}");

        markdown.AppendLine();

        // Meeting Notes
        if (notes != null)
        {
            if (!string.IsNullOrEmpty(notes.Summary))
            {
                markdown.AppendLine("## Summary");
                markdown.AppendLine();
                markdown.AppendLine(notes.Summary);
                markdown.AppendLine();
            }

            if (!string.IsNullOrEmpty(notes.KeyPoints))
            {
                markdown.AppendLine("## Key Points");
                markdown.AppendLine();
                var keyPoints = notes.KeyPoints.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                foreach (var point in keyPoints)
                {
                    markdown.AppendLine($"- {point}");
                }
                markdown.AppendLine();
            }

            if (!string.IsNullOrEmpty(notes.ActionItems))
            {
                markdown.AppendLine("## Action Items");
                markdown.AppendLine();
                var actionItems = notes.ActionItems.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                foreach (var item in actionItems)
                {
                    markdown.AppendLine($"- [ ] {item}");
                }
                markdown.AppendLine();
            }

            if (!string.IsNullOrEmpty(notes.Decisions))
            {
                markdown.AppendLine("## Decisions");
                markdown.AppendLine();
                markdown.AppendLine(notes.Decisions);
                markdown.AppendLine();
            }
        }

        // Conversation History
        if (exchanges != null && exchanges.Any())
        {
            markdown.AppendLine("## Conversation Transcript");
            markdown.AppendLine();

            foreach (var exchange in exchanges.OrderBy(e => e.Sequence))
            {
                markdown.AppendLine($"### Exchange {exchange.Sequence}");
                markdown.AppendLine();
                markdown.AppendLine($"**User ({exchange.InputTimestamp:HH:mm:ss}):**");
                markdown.AppendLine(exchange.UserInput);
                markdown.AppendLine();

                if (!string.IsNullOrEmpty(exchange.AiResponse))
                {
                    markdown.AppendLine($"**Assistant ({exchange.ResponseTimestamp:HH:mm:ss}):**");
                    markdown.AppendLine(exchange.AiResponse);
                    markdown.AppendLine();
                }
            }
        }

        markdown.AppendLine("---");
        markdown.AppendLine($"*Generated on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC*");

        return markdown.ToString();
    }

    public static string FormatPlainText(Meeting meeting, MeetingNotes? notes, IEnumerable<ConversationExchange>? exchanges)
    {
        var text = new StringBuilder();

        // Title
        text.AppendLine(meeting.Title.ToUpperInvariant());
        text.AppendLine(new string('=', meeting.Title.Length));
        text.AppendLine();

        // Metadata
        text.AppendLine("MEETING INFORMATION");
        text.AppendLine(new string('-', 20));
        text.AppendLine($"Date: {meeting.StartTime:yyyy-MM-dd HH:mm:ss}");
        
        if (meeting.EndTime.HasValue)
        {
            text.AppendLine($"End Time: {meeting.EndTime.Value:yyyy-MM-dd HH:mm:ss}");
            text.AppendLine($"Duration: {meeting.Duration ?? 0} seconds");
        }
        
        if (!string.IsNullOrEmpty(meeting.Type))
            text.AppendLine($"Type: {meeting.Type}");
        
        if (!string.IsNullOrEmpty(meeting.Domain))
            text.AppendLine($"Domain: {meeting.Domain}");
        
        if (!string.IsNullOrEmpty(meeting.Participants))
            text.AppendLine($"Participants: {meeting.Participants}");

        text.AppendLine();

        // Meeting Notes
        if (notes != null)
        {
            if (!string.IsNullOrEmpty(notes.Summary))
            {
                text.AppendLine("SUMMARY");
                text.AppendLine(new string('-', 20));
                text.AppendLine(notes.Summary);
                text.AppendLine();
            }

            if (!string.IsNullOrEmpty(notes.KeyPoints))
            {
                text.AppendLine("KEY POINTS");
                text.AppendLine(new string('-', 20));
                var keyPoints = notes.KeyPoints.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                foreach (var point in keyPoints)
                {
                    text.AppendLine($"• {point}");
                }
                text.AppendLine();
            }

            if (!string.IsNullOrEmpty(notes.ActionItems))
            {
                text.AppendLine("ACTION ITEMS");
                text.AppendLine(new string('-', 20));
                var actionItems = notes.ActionItems.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                foreach (var item in actionItems)
                {
                    text.AppendLine($"☐ {item}");
                }
                text.AppendLine();
            }
        }

        // Conversation History
        if (exchanges != null && exchanges.Any())
        {
            text.AppendLine("CONVERSATION TRANSCRIPT");
            text.AppendLine(new string('-', 20));
            text.AppendLine();

            foreach (var exchange in exchanges.OrderBy(e => e.Sequence))
            {
                text.AppendLine($"[{exchange.InputTimestamp:HH:mm:ss}] USER:");
                text.AppendLine(exchange.UserInput);
                text.AppendLine();

                if (!string.IsNullOrEmpty(exchange.AiResponse))
                {
                    text.AppendLine($"[{exchange.ResponseTimestamp:HH:mm:ss}] ASSISTANT:");
                    text.AppendLine(exchange.AiResponse);
                    text.AppendLine();
                }

                text.AppendLine(new string('-', 40));
                text.AppendLine();
            }
        }

        text.AppendLine($"Generated on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");

        return text.ToString();
    }
}
