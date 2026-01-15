using AudioAssistant.Api.Data;
using AudioAssistant.Api.Models;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace AudioAssistant.Api.Services;

public class ConversationService : IConversationService
{
    private readonly AudioAssistantDbContext _context;
    private readonly ILogger<ConversationService> _logger;

    public ConversationService(
        AudioAssistantDbContext context,
        ILogger<ConversationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Conversation> CreateConversationAsync(
        int userId,
        string? meetingType = null,
        string? domain = null,
        CancellationToken cancellationToken = default)
    {
        var conversation = new Conversation
        {
            UserId = userId,
            SessionId = Guid.NewGuid().ToString(),
            StartedAt = DateTime.UtcNow,
            MeetingType = meetingType ?? "general",
            Domain = domain ?? "general",
            Language = "en"
        };

        _context.Conversations.Add(conversation);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created conversation {ConversationId} for user {UserId}", conversation.Id, userId);

        return conversation;
    }

    public async Task<ConversationExchange> AddExchangeAsync(
        int conversationId,
        string userInput,
        string? aiResponse = null,
        string? responseStyle = null,
        string? aiProvider = null,
        string? context = null,
        CancellationToken cancellationToken = default)
    {
        var conversation = await _context.Conversations
            .Include(c => c.Exchanges)
            .FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken);

        if (conversation == null)
        {
            throw new InvalidOperationException($"Conversation {conversationId} not found");
        }

        var sequence = conversation.Exchanges.Count + 1;

        var exchange = new ConversationExchange
        {
            ConversationId = conversationId,
            Sequence = sequence,
            UserInput = userInput,
            InputTimestamp = DateTime.UtcNow,
            AiResponse = aiResponse,
            ResponseTimestamp = aiResponse != null ? DateTime.UtcNow : null,
            ResponseStyle = responseStyle,
            AiProvider = aiProvider,
            ContextUsed = context
        };

        _context.ConversationExchanges.Add(exchange);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Added exchange {Sequence} to conversation {ConversationId}", sequence, conversationId);

        return exchange;
    }

    public async Task<Conversation?> GetConversationAsync(
        int conversationId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Conversations
            .Include(c => c.Exchanges.OrderBy(e => e.Sequence))
            .Include(c => c.Meeting)
            .FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken);
    }

    public async Task<IEnumerable<ConversationExchange>> GetConversationHistoryAsync(
        int conversationId,
        int limit = 25,
        CancellationToken cancellationToken = default)
    {
        return await _context.ConversationExchanges
            .Where(e => e.ConversationId == conversationId)
            .OrderByDescending(e => e.Sequence)
            .Take(limit)
            .OrderBy(e => e.Sequence)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateContextAsync(
        int conversationId,
        string? meetingType = null,
        string? domain = null,
        string? summary = null,
        CancellationToken cancellationToken = default)
    {
        var conversation = await _context.Conversations
            .FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken);

        if (conversation == null)
        {
            throw new InvalidOperationException($"Conversation {conversationId} not found");
        }

        if (meetingType != null)
            conversation.MeetingType = meetingType;

        if (domain != null)
            conversation.Domain = domain;

        if (summary != null)
            conversation.Summary = summary;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated context for conversation {ConversationId}", conversationId);
    }

    public async Task EndConversationAsync(
        int conversationId,
        CancellationToken cancellationToken = default)
    {
        var conversation = await _context.Conversations
            .FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken);

        if (conversation == null)
        {
            throw new InvalidOperationException($"Conversation {conversationId} not found");
        }

        conversation.EndedAt = DateTime.UtcNow;
        conversation.Duration = (int)(conversation.EndedAt.Value - conversation.StartedAt).TotalSeconds;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Ended conversation {ConversationId} with duration {Duration}s", conversationId, conversation.Duration);
    }

    public async Task<string> GetAggregatedContextAsync(
        int conversationId,
        CancellationToken cancellationToken = default)
    {
        var conversation = await GetConversationAsync(conversationId, cancellationToken);

        if (conversation == null)
        {
            return string.Empty;
        }

        var context = new StringBuilder();

        context.AppendLine($"Meeting Type: {conversation.MeetingType}");
        context.AppendLine($"Domain: {conversation.Domain}");

        if (!string.IsNullOrEmpty(conversation.Summary))
        {
            context.AppendLine($"Summary: {conversation.Summary}");
        }

        var recentExchanges = conversation.Exchanges
            .OrderByDescending(e => e.Sequence)
            .Take(10)
            .OrderBy(e => e.Sequence)
            .ToList();

        if (recentExchanges.Any())
        {
            context.AppendLine("\nRecent conversation:");
            foreach (var exchange in recentExchanges)
            {
                context.AppendLine($"User: {exchange.UserInput}");
                if (!string.IsNullOrEmpty(exchange.AiResponse))
                {
                    context.AppendLine($"Assistant: {exchange.AiResponse}");
                }
            }
        }

        return context.ToString();
    }
}
