using AudioAssistant.Api.Models;

namespace AudioAssistant.Api.Services;

/// <summary>
/// Service for managing conversation sessions
/// </summary>
public interface IConversationService
{
    /// <summary>
    /// Create a new conversation
    /// </summary>
    Task<Conversation> CreateConversationAsync(
        int userId,
        string? meetingType = null,
        string? domain = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Add an exchange to a conversation
    /// </summary>
    Task<ConversationExchange> AddExchangeAsync(
        int conversationId,
        string userInput,
        string? aiResponse = null,
        string? responseStyle = null,
        string? aiProvider = null,
        string? context = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a conversation by ID
    /// </summary>
    Task<Conversation?> GetConversationAsync(
        int conversationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get conversation history (recent exchanges)
    /// </summary>
    Task<IEnumerable<ConversationExchange>> GetConversationHistoryAsync(
        int conversationId,
        int limit = 25,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update conversation context
    /// </summary>
    Task UpdateContextAsync(
        int conversationId,
        string? meetingType = null,
        string? domain = null,
        string? summary = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// End a conversation
    /// </summary>
    Task EndConversationAsync(
        int conversationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get aggregated context for AI responses
    /// </summary>
    Task<string> GetAggregatedContextAsync(
        int conversationId,
        CancellationToken cancellationToken = default);
}
