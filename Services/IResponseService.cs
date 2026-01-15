namespace AudioAssistant.Api.Services;

/// <summary>
/// Service for generating AI responses to transcribed text
/// </summary>
public interface IResponseService
{
    /// <summary>
    /// Generate an AI response based on the provided transcript
    /// </summary>
    Task<ResponseResult> GenerateResponseAsync(
        string transcript,
        int userId,
        int? conversationId = null,
        string? responseStyle = null,
        string? aiProvider = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get available AI providers
    /// </summary>
    Task<IEnumerable<string>> GetAvailableProvidersAsync(int userId);

    /// <summary>
    /// Get available response styles
    /// </summary>
    Task<IEnumerable<ResponseStyleInfo>> GetAvailableStylesAsync();
}

/// <summary>
/// Result of AI response generation
/// </summary>
public class ResponseResult
{
    public string Response { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string Style { get; set; } = string.Empty;
    public int TokensUsed { get; set; }
    public decimal Cost { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Information about a response style
/// </summary>
public class ResponseStyleInfo
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string SystemPromptModifier { get; set; } = string.Empty;
}
