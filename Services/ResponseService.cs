using AudioAssistant.Api.Data;
using AudioAssistant.Api.Models;
using AudioAssistant.Api.Utilities;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

namespace AudioAssistant.Api.Services;

public class ResponseService : IResponseService
{
    private readonly AudioAssistantDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly EncryptionService _encryptionService;
    private readonly ILogger<ResponseService> _logger;
    private readonly IConfiguration _configuration;

    private static readonly Dictionary<string, ResponseStyleInfo> _responseStyles = new()
    {
        ["formal"] = new ResponseStyleInfo
        {
            Name = "formal",
            DisplayName = "Formal",
            Description = "Professional, business-appropriate tone",
            SystemPromptModifier = "Respond in a formal, professional manner. Use proper grammar, avoid contractions, and maintain a business-appropriate tone."
        },
        ["casual"] = new ResponseStyleInfo
        {
            Name = "casual",
            DisplayName = "Casual",
            Description = "Friendly, conversational tone",
            SystemPromptModifier = "Respond in a casual, friendly manner. Use conversational language and feel free to use contractions."
        },
        ["technical"] = new ResponseStyleInfo
        {
            Name = "technical",
            DisplayName = "Technical",
            Description = "Detailed technical explanations with terminology",
            SystemPromptModifier = "Respond with technical precision. Use appropriate terminology, include implementation details, and be specific about technical concepts."
        },
        ["eli5"] = new ResponseStyleInfo
        {
            Name = "eli5",
            DisplayName = "ELI5 (Explain Like I'm 5)",
            Description = "Simple explanations that anyone can understand",
            SystemPromptModifier = "Explain like I'm five years old. Use simple words, relatable analogies, and avoid jargon. Make it easy for anyone to understand."
        },
        ["funny"] = new ResponseStyleInfo
        {
            Name = "funny",
            DisplayName = "Funny",
            Description = "Humorous, entertaining responses",
            SystemPromptModifier = "Respond with humor and wit. Include jokes, puns, or funny observations while still being informative."
        },
        ["bulletpoints"] = new ResponseStyleInfo
        {
            Name = "bulletpoints",
            DisplayName = "Bullet Points",
            Description = "Concise, structured bullet point format",
            SystemPromptModifier = "Structure your response using bullet points. Be concise and organize information in a clear, scannable format."
        }
    };

    public ResponseService(
        AudioAssistantDbContext context,
        IHttpClientFactory httpClientFactory,
        EncryptionService encryptionService,
        ILogger<ResponseService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _encryptionService = encryptionService;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<ResponseResult> GenerateResponseAsync(
        string transcript,
        int userId,
        int? conversationId = null,
        string? responseStyle = null,
        string? aiProvider = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get user preferences
            var preferences = await _context.UserPreferences
                .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

            var style = responseStyle ?? preferences?.PreferredResponseStyle ?? "formal";
            var provider = aiProvider ?? preferences?.PreferredAIProvider ?? "claude";

            // Get conversation context if applicable
            string? conversationContext = null;
            if (conversationId.HasValue)
            {
                conversationContext = await GetConversationContextAsync(conversationId.Value, cancellationToken);
            }

            // Build system prompt with style modifier
            var systemPrompt = BuildSystemPrompt(style, conversationContext);

            // Try providers in fallback order
            var providers = new[] { provider, "claude", "gpt4", "gemini" }.Distinct();
            
            foreach (var currentProvider in providers)
            {
                var result = await TryGenerateResponseAsync(
                    currentProvider,
                    userId,
                    transcript,
                    systemPrompt,
                    style,
                    cancellationToken);

                if (result.Success)
                {
                    // Log transaction
                    await LogTransactionAsync(userId, currentProvider, result.TokensUsed, result.Cost, cancellationToken);
                    return result;
                }
            }

            return new ResponseResult
            {
                Success = false,
                ErrorMessage = "All AI providers failed to generate a response"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating AI response for user {UserId}", userId);
            return new ResponseResult
            {
                Success = false,
                ErrorMessage = $"Error generating response: {ex.Message}"
            };
        }
    }

    public async Task<IEnumerable<string>> GetAvailableProvidersAsync(int userId)
    {
        var providers = new List<string>();

        // Check for API keys
        var apiKeys = await _context.ApiKeys
            .Where(k => k.UserId == userId && k.IsActive)
            .Select(k => k.Provider)
            .ToListAsync();

        if (apiKeys.Contains("claude")) providers.Add("claude");
        if (apiKeys.Contains("openai")) providers.Add("gpt4");
        if (apiKeys.Contains("gemini") || apiKeys.Contains("google")) providers.Add("gemini");

        return providers;
    }

    public Task<IEnumerable<ResponseStyleInfo>> GetAvailableStylesAsync()
    {
        return Task.FromResult(_responseStyles.Values.AsEnumerable());
    }

    private async Task<ResponseResult> TryGenerateResponseAsync(
        string provider,
        int userId,
        string transcript,
        string systemPrompt,
        string style,
        CancellationToken cancellationToken)
    {
        try
        {
            return provider.ToLowerInvariant() switch
            {
                "claude" => await GenerateWithClaudeAsync(userId, transcript, systemPrompt, style, cancellationToken),
                "gpt4" or "openai" => await GenerateWithGPT4Async(userId, transcript, systemPrompt, style, cancellationToken),
                "gemini" or "google" => await GenerateWithGeminiAsync(userId, transcript, systemPrompt, style, cancellationToken),
                _ => new ResponseResult { Success = false, ErrorMessage = $"Unknown provider: {provider}" }
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Provider {Provider} failed for user {UserId}", provider, userId);
            return new ResponseResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    private async Task<ResponseResult> GenerateWithClaudeAsync(
        int userId,
        string transcript,
        string systemPrompt,
        string style,
        CancellationToken cancellationToken)
    {
        var apiKey = await GetDecryptedApiKeyAsync(userId, "claude", cancellationToken);
        if (string.IsNullOrEmpty(apiKey))
        {
            return new ResponseResult { Success = false, ErrorMessage = "Claude API key not found" };
        }

        var httpClient = _httpClientFactory.CreateClient();
        httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
        httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

        var requestBody = new
        {
            model = "claude-3-haiku-20240307",
            max_tokens = 1024,
            system = systemPrompt,
            messages = new[]
            {
                new { role = "user", content = transcript }
            }
        };

        var response = await httpClient.PostAsync(
            "https://api.anthropic.com/v1/messages",
            new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"),
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Claude API error: {Error}", error);
            return new ResponseResult { Success = false, ErrorMessage = $"Claude API error: {response.StatusCode}" };
        }

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var jsonDoc = JsonDocument.Parse(responseContent);
        
        var text = jsonDoc.RootElement.GetProperty("content")[0].GetProperty("text").GetString() ?? "";
        var inputTokens = jsonDoc.RootElement.GetProperty("usage").GetProperty("input_tokens").GetInt32();
        var outputTokens = jsonDoc.RootElement.GetProperty("usage").GetProperty("output_tokens").GetInt32();
        var totalTokens = inputTokens + outputTokens;

        // Claude Haiku pricing: $0.25 per 1M input tokens, $1.25 per 1M output tokens
        var cost = (inputTokens * 0.25m / 1_000_000m) + (outputTokens * 1.25m / 1_000_000m);

        return new ResponseResult
        {
            Response = text,
            Provider = "claude",
            Style = style,
            TokensUsed = totalTokens,
            Cost = cost,
            Success = true
        };
    }

    private async Task<ResponseResult> GenerateWithGPT4Async(
        int userId,
        string transcript,
        string systemPrompt,
        string style,
        CancellationToken cancellationToken)
    {
        var apiKey = await GetDecryptedApiKeyAsync(userId, "openai", cancellationToken);
        if (string.IsNullOrEmpty(apiKey))
        {
            return new ResponseResult { Success = false, ErrorMessage = "OpenAI API key not found" };
        }

        var httpClient = _httpClientFactory.CreateClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

        var requestBody = new
        {
            model = "gpt-4-turbo-preview",
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = transcript }
            },
            max_tokens = 1024
        };

        var response = await httpClient.PostAsync(
            "https://api.openai.com/v1/chat/completions",
            new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"),
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("OpenAI API error: {Error}", error);
            return new ResponseResult { Success = false, ErrorMessage = $"OpenAI API error: {response.StatusCode}" };
        }

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var jsonDoc = JsonDocument.Parse(responseContent);
        
        var text = jsonDoc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";
        var totalTokens = jsonDoc.RootElement.GetProperty("usage").GetProperty("total_tokens").GetInt32();
        var promptTokens = jsonDoc.RootElement.GetProperty("usage").GetProperty("prompt_tokens").GetInt32();
        var completionTokens = jsonDoc.RootElement.GetProperty("usage").GetProperty("completion_tokens").GetInt32();

        // GPT-4 pricing: $10 per 1M input tokens, $30 per 1M output tokens
        var cost = (promptTokens * 10m / 1_000_000m) + (completionTokens * 30m / 1_000_000m);

        return new ResponseResult
        {
            Response = text,
            Provider = "gpt4",
            Style = style,
            TokensUsed = totalTokens,
            Cost = cost,
            Success = true
        };
    }

    private async Task<ResponseResult> GenerateWithGeminiAsync(
        int userId,
        string transcript,
        string systemPrompt,
        string style,
        CancellationToken cancellationToken)
    {
        var apiKey = await GetDecryptedApiKeyAsync(userId, "gemini", cancellationToken);
        if (string.IsNullOrEmpty(apiKey))
        {
            apiKey = await GetDecryptedApiKeyAsync(userId, "google", cancellationToken);
        }
        
        if (string.IsNullOrEmpty(apiKey))
        {
            return new ResponseResult { Success = false, ErrorMessage = "Gemini API key not found" };
        }

        var httpClient = _httpClientFactory.CreateClient();

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = $"{systemPrompt}\n\nUser: {transcript}" }
                    }
                }
            },
            generationConfig = new
            {
                maxOutputTokens = 1024,
                temperature = 0.7
            }
        };

        var response = await httpClient.PostAsync(
            $"https://generativelanguage.googleapis.com/v1beta/models/gemini-pro:generateContent?key={apiKey}",
            new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"),
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Gemini API error: {Error}", error);
            return new ResponseResult { Success = false, ErrorMessage = $"Gemini API error: {response.StatusCode}" };
        }

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var jsonDoc = JsonDocument.Parse(responseContent);
        
        var text = jsonDoc.RootElement.GetProperty("candidates")[0]
            .GetProperty("content").GetProperty("parts")[0]
            .GetProperty("text").GetString() ?? "";

        // Estimate tokens (Gemini doesn't always return token count)
        var estimatedTokens = (transcript.Length + text.Length) / 4;
        
        // Gemini pricing: roughly $0.50 per 1M tokens
        var cost = estimatedTokens * 0.50m / 1_000_000m;

        return new ResponseResult
        {
            Response = text,
            Provider = "gemini",
            Style = style,
            TokensUsed = estimatedTokens,
            Cost = cost,
            Success = true
        };
    }

    private string BuildSystemPrompt(string style, string? conversationContext)
    {
        var basePrompt = "You are a helpful AI assistant that responds to transcribed audio input. ";
        basePrompt += "Provide accurate, helpful, and contextually relevant responses. ";

        if (_responseStyles.TryGetValue(style.ToLowerInvariant(), out var styleInfo))
        {
            basePrompt += styleInfo.SystemPromptModifier + " ";
        }

        if (!string.IsNullOrEmpty(conversationContext))
        {
            basePrompt += $"\n\nConversation context:\n{conversationContext}";
        }

        return basePrompt;
    }

    private async Task<string?> GetConversationContextAsync(int conversationId, CancellationToken cancellationToken)
    {
        var exchanges = await _context.ConversationExchanges
            .Where(e => e.ConversationId == conversationId)
            .OrderByDescending(e => e.Sequence)
            .Take(5) // Last 5 exchanges
            .OrderBy(e => e.Sequence)
            .ToListAsync(cancellationToken);

        if (!exchanges.Any())
            return null;

        var context = new StringBuilder();
        foreach (var exchange in exchanges)
        {
            context.AppendLine($"User: {exchange.UserInput}");
            if (!string.IsNullOrEmpty(exchange.AiResponse))
            {
                context.AppendLine($"Assistant: {exchange.AiResponse}");
            }
        }

        return context.ToString();
    }

    private async Task<string?> GetDecryptedApiKeyAsync(int userId, string provider, CancellationToken cancellationToken)
    {
        var apiKey = await _context.ApiKeys
            .FirstOrDefaultAsync(k => k.UserId == userId && k.Provider.ToLower() == provider.ToLower() && k.IsActive, cancellationToken);

        if (apiKey == null || string.IsNullOrEmpty(apiKey.EncryptedKey))
            return null;

        return _encryptionService.Decrypt(apiKey.EncryptedKey);
    }

    private async Task LogTransactionAsync(
        int userId,
        string provider,
        int tokensUsed,
        decimal cost,
        CancellationToken cancellationToken)
    {
        var transaction = new TransactionLog
        {
            UserId = userId,
            TransactionType = "ai_response",
            Provider = provider,
            TokensUsed = tokensUsed,
            Cost = cost,
            Timestamp = DateTime.UtcNow
        };

        _context.TransactionLogs.Add(transaction);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
