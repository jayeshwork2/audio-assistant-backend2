using AudioAssistant.Api.Data;
using AudioAssistant.Api.Models;
using AudioAssistant.Api.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.RegularExpressions;

namespace AudioAssistant.Api.Services;

public class MeetingService : IMeetingService
{
    private readonly AudioAssistantDbContext _context;
    private readonly ILogger<MeetingService> _logger;

    // Keywords for meeting type detection
    private static readonly Dictionary<string, string[]> _meetingTypeKeywords = new()
    {
        ["interview"] = new[] { "interview", "candidate", "position", "role", "hire", "hiring", "resume", "experience", "qualification" },
        ["sales"] = new[] { "sales", "product", "demo", "pricing", "contract", "purchase", "buy", "customer", "client", "revenue", "deal", "proposal" },
        ["training"] = new[] { "training", "learn", "teach", "tutorial", "workshop", "course", "lesson", "skill", "practice", "exercise" },
        ["standup"] = new[] { "standup", "daily", "yesterday", "today", "blocker", "sprint", "scrum", "update", "progress" },
        ["meeting"] = new[] { "meeting", "discuss", "agenda", "topic", "presentation", "review" }
    };

    // Keywords for domain detection
    private static readonly Dictionary<string, string[]> _domainKeywords = new()
    {
        ["technical"] = new[] { "code", "api", "database", "server", "software", "bug", "deploy", "git", "docker", "kubernetes", "aws", "cloud", "architecture", "algorithm", "framework" },
        ["business"] = new[] { "revenue", "profit", "strategy", "market", "customer", "growth", "sales", "business", "roi", "kpi", "metrics", "stakeholder", "investment" },
        ["legal"] = new[] { "legal", "contract", "law", "compliance", "regulation", "terms", "agreement", "clause", "liability", "attorney", "court" },
        ["medical"] = new[] { "patient", "doctor", "medical", "health", "diagnosis", "treatment", "symptoms", "medication", "hospital", "clinic", "therapy" },
        ["finance"] = new[] { "finance", "budget", "cost", "expense", "accounting", "payment", "invoice", "financial", "tax", "audit" },
        ["marketing"] = new[] { "marketing", "campaign", "brand", "social media", "advertising", "seo", "content", "engagement", "analytics" },
        ["hr"] = new[] { "hr", "human resources", "employee", "recruitment", "onboarding", "performance", "benefits", "payroll", "culture" }
    };

    // Keywords for formality detection
    private static readonly string[] _formalIndicators = new[]
    {
        "hereby", "pursuant", "therefore", "furthermore", "regarding", "kindly", "sincerely",
        "respectfully", "gentleman", "madam", "sir", "pleased to", "would appreciate"
    };

    private static readonly string[] _informalIndicators = new[]
    {
        "hey", "yeah", "yup", "nope", "gonna", "wanna", "gotta", "cool", "awesome",
        "nice", "thanks", "thx", "btw", "lol", "omg"
    };

    // Keywords for urgency detection
    private static readonly string[] _urgentKeywords = new[]
    {
        "urgent", "asap", "immediately", "critical", "emergency", "deadline", "now",
        "today", "tonight", "must", "crucial", "important", "priority", "rush"
    };

    // Action item indicators
    private static readonly string[] _actionItemPatterns = new[]
    {
        @"\b(will|shall|need to|must|should|have to|going to|plan to)\s+\w+",
        @"\b(todo|to do|action item|task|follow up|follow-up)\b",
        @"\b(assign|assigned|responsible for|owner)\b"
    };

    public MeetingService(
        AudioAssistantDbContext context,
        ILogger<MeetingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Meeting> CreateMeetingAsync(
        string conversationId,
        string title,
        CancellationToken cancellationToken = default)
    {
        // Look up conversation by SessionId
        var conversation = await _context.Conversations
            .FirstOrDefaultAsync(c => c.SessionId == conversationId, cancellationToken);

        if (conversation == null)
        {
             // If conversation doesn't exist, create it on the fly (for MVP)
             // In a real app, conversation should probably exist before meeting creation
             conversation = new Conversation
             {
                 SessionId = conversationId,
                 UserId = 1, // Default user for No-Auth
                 StartedAt = DateTime.UtcNow,
                 Title = "New Conversation" 
             };
             _context.Conversations.Add(conversation);
             await _context.SaveChangesAsync(cancellationToken);
        }

        var meeting = new Meeting
        {
            ConversationId = conversation.Id,
            Title = title,
            StartTime = conversation.StartedAt,
            Type = conversation.MeetingType,
            Domain = conversation.Domain
        };

        _context.Meetings.Add(meeting);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created meeting {MeetingId} for conversation {ConversationId}", meeting.Id, conversationId);

        return meeting;
    }

    public async Task<MeetingDetectionResult> AnalyzeMeetingAsync(
        string transcript,
        CancellationToken cancellationToken = default)
    {
        var type = await DetectMeetingTypeAsync(transcript, cancellationToken);
        var domain = await DetectDomainAsync(transcript, cancellationToken);
        var formality = await DetectFormalityAsync(transcript, cancellationToken);
        var urgency = await DetectUrgencyAsync(transcript, cancellationToken);
        
        // Basic extraction for now
        var keyPoints = await ExtractKeyPointsAsync(transcript, cancellationToken);
        var actionItems = await ExtractActionItemsAsync(transcript, cancellationToken);

        return new MeetingDetectionResult
        {
             MeetingType = type,
             Domain = domain,
             Formality = formality,
             Urgency = urgency,
             KeyPoints = keyPoints,
             ActionItems = actionItems,
             Summary = "Generated from transcript analysis"
        };
    }

    public Task<string> DetectMeetingTypeAsync(
        string transcript,
        CancellationToken cancellationToken = default)
    {
        var lowerTranscript = transcript.ToLowerInvariant();
        var scores = new Dictionary<string, int>();

        foreach (var (type, keywords) in _meetingTypeKeywords)
        {
            var score = keywords.Count(keyword => lowerTranscript.Contains(keyword));
            scores[type] = score;
        }

        var detectedType = scores.OrderByDescending(s => s.Value).First();
        return Task.FromResult(detectedType.Value > 0 ? detectedType.Key : "meeting");
    }

    public Task<string> DetectDomainAsync(
        string transcript,
        CancellationToken cancellationToken = default)
    {
        var lowerTranscript = transcript.ToLowerInvariant();
        var scores = new Dictionary<string, int>();

        foreach (var (domain, keywords) in _domainKeywords)
        {
            var score = keywords.Count(keyword => lowerTranscript.Contains(keyword));
            scores[domain] = score;
        }

        var detectedDomain = scores.OrderByDescending(s => s.Value).First();
        return Task.FromResult(detectedDomain.Value > 0 ? detectedDomain.Key : "general");
    }

    public Task<string> DetectFormalityAsync(
        string transcript,
        CancellationToken cancellationToken = default)
    {
        var lowerTranscript = transcript.ToLowerInvariant();

        var formalCount = _formalIndicators.Count(indicator => lowerTranscript.Contains(indicator));
        var informalCount = _informalIndicators.Count(indicator => lowerTranscript.Contains(indicator));

        if (formalCount > informalCount * 2)
            return Task.FromResult("formal");
        else if (informalCount > formalCount * 2)
            return Task.FromResult("informal");
        else
            return Task.FromResult("mixed");
    }

    public Task<string> DetectUrgencyAsync(
        string transcript,
        CancellationToken cancellationToken = default)
    {
        var lowerTranscript = transcript.ToLowerInvariant();
        var urgentCount = _urgentKeywords.Count(keyword => lowerTranscript.Contains(keyword));

        // Check for repeated exclamation marks
        var exclamationCount = transcript.Count(c => c == '!');

        var urgencyScore = urgentCount + (exclamationCount / 3);

        if (urgencyScore >= 3)
            return Task.FromResult("high");
        else if (urgencyScore >= 1)
            return Task.FromResult("medium");
        else
            return Task.FromResult("low");
    }

    public async Task<MeetingNotes> GenerateMeetingNotesAsync(
        int meetingId,
        CancellationToken cancellationToken = default)
    {
        var meeting = await _context.Meetings
            .Include(m => m.Conversation)
                .ThenInclude(c => c.Exchanges)
            .Include(m => m.Conversation)
                .ThenInclude(c => c.Transcripts)
            .FirstOrDefaultAsync(m => m.Id == meetingId, cancellationToken);

        if (meeting == null)
        {
            throw new InvalidOperationException($"Meeting {meetingId} not found");
        }

        // Aggregate all transcripts and exchanges
        var allText = new StringBuilder();

        foreach (var transcript in meeting.Conversation.Transcripts)
        {
            allText.AppendLine(transcript.RawTranscript);
        }

        foreach (var exchange in meeting.Conversation.Exchanges)
        {
            allText.AppendLine(exchange.UserInput);
            if (!string.IsNullOrEmpty(exchange.AiResponse))
            {
                allText.AppendLine(exchange.AiResponse);
            }
        }

        var fullTranscript = allText.ToString();

        // Extract information
        var actionItems = await ExtractActionItemsAsync(fullTranscript, cancellationToken);
        var keyPoints = await ExtractKeyPointsAsync(fullTranscript, cancellationToken);
        var summary = GenerateSummary(fullTranscript, keyPoints);

        // Check if notes already exist
        var existingNotes = await _context.MeetingNotes
            .FirstOrDefaultAsync(n => n.MeetingId == meetingId, cancellationToken);

        if (existingNotes != null)
        {
            existingNotes.Summary = summary;
            existingNotes.KeyPoints = string.Join("\n", keyPoints);
            existingNotes.ActionItems = string.Join("\n", actionItems);
            existingNotes.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            existingNotes = new MeetingNotes
            {
                MeetingId = meetingId,
                Summary = summary,
                KeyPoints = string.Join("\n", keyPoints),
                ActionItems = string.Join("\n", actionItems),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.MeetingNotes.Add(existingNotes);
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Generated meeting notes for meeting {MeetingId}", meetingId);

        return existingNotes;
    }

    public Task<List<string>> ExtractActionItemsAsync(
        string transcript,
        CancellationToken cancellationToken = default)
    {
        var actionItems = new List<string>();
        var sentences = SplitIntoSentences(transcript);

        foreach (var sentence in sentences)
        {
            foreach (var pattern in _actionItemPatterns)
            {
                if (Regex.IsMatch(sentence, pattern, RegexOptions.IgnoreCase))
                {
                    var cleaned = sentence.Trim();
                    if (cleaned.Length > 10 && cleaned.Length < 200)
                    {
                        actionItems.Add(cleaned);
                        break;
                    }
                }
            }
        }

        // Remove duplicates and limit
        return Task.FromResult(actionItems.Distinct().Take(10).ToList());
    }

    public Task<List<string>> ExtractKeyPointsAsync(
        string transcript,
        CancellationToken cancellationToken = default)
    {
        var keyPoints = new List<string>();
        var sentences = SplitIntoSentences(transcript);

        // Look for sentences with important keywords
        var importantKeywords = new[]
        {
            "important", "key", "critical", "main", "primary", "essential", "significant",
            "decided", "agreed", "concluded", "determined", "resolved"
        };

        foreach (var sentence in sentences)
        {
            var lowerSentence = sentence.ToLowerInvariant();
            if (importantKeywords.Any(keyword => lowerSentence.Contains(keyword)))
            {
                var cleaned = sentence.Trim();
                if (cleaned.Length > 20 && cleaned.Length < 200)
                {
                    keyPoints.Add(cleaned);
                }
            }
        }

        // If no key points found, take first few substantial sentences
        if (keyPoints.Count == 0)
        {
            keyPoints = sentences
                .Where(s => s.Length > 30 && s.Length < 200)
                .Take(5)
                .ToList();
        }

        return Task.FromResult(keyPoints.Distinct().Take(8).ToList());
    }

    public async Task<Meeting?> GetMeetingAsync(
        int meetingId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Meetings
            .Include(m => m.Notes)
            .Include(m => m.Conversation)
            .FirstOrDefaultAsync(m => m.Id == meetingId, cancellationToken);
    }

    public async Task<string> GetMeetingSummaryAsync(
        int meetingId,
        CancellationToken cancellationToken = default)
    {
        var notes = await _context.MeetingNotes
            .FirstOrDefaultAsync(n => n.MeetingId == meetingId, cancellationToken);

        return notes?.Summary ?? "No summary available";
    }

    private string GenerateSummary(string fullText, List<string> keyPoints)
    {
        var summary = new StringBuilder();

        // Take first 200 characters as intro
        var intro = fullText.Length > 200
            ? fullText.Substring(0, 200) + "..."
            : fullText;

        summary.AppendLine(intro);

        if (keyPoints.Any())
        {
            summary.AppendLine("\nKey discussion points:");
            foreach (var point in keyPoints.Take(3))
            {
                summary.AppendLine($"- {point}");
            }
        }

        return summary.ToString();
    }

    private List<string> SplitIntoSentences(string text)
    {
        // Split by common sentence delimiters
        var sentences = Regex.Split(text, @"[.!?]+")
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())
            .ToList();

        return sentences;
    }
}
