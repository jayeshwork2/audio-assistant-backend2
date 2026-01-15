using AudioAssistant.Api.Data;
using AudioAssistant.Api.Models;
using AudioAssistant.Api.Services;
using AudioAssistant.Api.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;
using Xunit;

namespace AudioAssistant.Tests;

public class ResponseServiceTests
{
    private readonly AudioAssistantDbContext _context;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly EncryptionService _encryptionService;
    private readonly Mock<ILogger<ResponseService>> _loggerMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly ResponseService _responseService;

    public ResponseServiceTests()
    {
        var options = new DbContextOptionsBuilder<AudioAssistantDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AudioAssistantDbContext(options);
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _encryptionService = new EncryptionService("test-encryption-key-minimum-32");
        _loggerMock = new Mock<ILogger<ResponseService>>();
        _configurationMock = new Mock<IConfiguration>();

        _responseService = new ResponseService(
            _context,
            _httpClientFactoryMock.Object,
            _encryptionService,
            _loggerMock.Object,
            _configurationMock.Object);
    }

    [Fact]
    public async Task GetAvailableStylesAsync_ShouldReturnAllStyles()
    {
        // Act
        var styles = await _responseService.GetAvailableStylesAsync();

        // Assert
        Assert.NotNull(styles);
        var styleList = styles.ToList();
        Assert.Equal(6, styleList.Count);
        Assert.Contains(styleList, s => s.Name == "formal");
        Assert.Contains(styleList, s => s.Name == "casual");
        Assert.Contains(styleList, s => s.Name == "technical");
        Assert.Contains(styleList, s => s.Name == "eli5");
        Assert.Contains(styleList, s => s.Name == "funny");
        Assert.Contains(styleList, s => s.Name == "bulletpoints");
    }

    [Fact]
    public async Task GetAvailableProvidersAsync_WithNoApiKeys_ReturnsEmptyList()
    {
        // Arrange
        var user = new User { Email = "test@example.com", PasswordHash = "hash" };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var providers = await _responseService.GetAvailableProvidersAsync(user.Id);

        // Assert
        Assert.NotNull(providers);
        Assert.Empty(providers);
    }

    [Fact]
    public async Task GetAvailableProvidersAsync_WithApiKeys_ReturnsProviders()
    {
        // Arrange
        var user = new User { Email = "test@example.com", PasswordHash = "hash" };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var apiKey1 = new ApiKey
        {
            UserId = user.Id,
            Provider = "claude",
            EncryptedKey = "encrypted",
            IsActive = true
        };
        var apiKey2 = new ApiKey
        {
            UserId = user.Id,
            Provider = "openai",
            EncryptedKey = "encrypted",
            IsActive = true
        };

        _context.ApiKeys.AddRange(apiKey1, apiKey2);
        await _context.SaveChangesAsync();

        // Act
        var providers = await _responseService.GetAvailableProvidersAsync(user.Id);

        // Assert
        var providerList = providers.ToList();
        Assert.Contains("claude", providerList);
        Assert.Contains("gpt4", providerList);
    }

    [Fact]
    public async Task GenerateResponseAsync_WithNoApiKeys_ReturnsFalse()
    {
        // Arrange
        var user = new User { Email = "test@example.com", PasswordHash = "hash" };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _responseService.GenerateResponseAsync(
            "Test transcript",
            user.Id);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task GenerateResponseAsync_WithClaudeApiKey_Success()
    {
        // Arrange
        var user = new User { Email = "test@example.com", PasswordHash = "hash" };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var encryptedKey = _encryptionService.Encrypt("test-api-key");
        var apiKey = new ApiKey
        {
            UserId = user.Id,
            Provider = "claude",
            EncryptedKey = encryptedKey,
            IsActive = true
        };
        _context.ApiKeys.Add(apiKey);
        await _context.SaveChangesAsync();

        // Mock HTTP response
        var mockResponse = new
        {
            content = new[] { new { text = "AI response" } },
            usage = new { input_tokens = 10, output_tokens = 20 }
        };

        var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(mockResponse))
            });

        var httpClient = new HttpClient(httpMessageHandlerMock.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act
        var result = await _responseService.GenerateResponseAsync(
            "Test transcript",
            user.Id,
            aiProvider: "claude");

        // Assert
        Assert.True(result.Success);
        Assert.Equal("AI response", result.Response);
        Assert.Equal("claude", result.Provider);
        Assert.Equal(30, result.TokensUsed);
    }

    [Fact]
    public async Task GenerateResponseAsync_WithConversationContext_IncludesContext()
    {
        // Arrange
        var user = new User { Email = "test@example.com", PasswordHash = "hash" };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var conversation = new Conversation
        {
            UserId = user.Id,
            SessionId = Guid.NewGuid().ToString(),
            MeetingType = "meeting",
            Domain = "technical"
        };
        _context.Conversations.Add(conversation);
        await _context.SaveChangesAsync();

        var exchange = new ConversationExchange
        {
            ConversationId = conversation.Id,
            Sequence = 1,
            UserInput = "Previous question",
            AiResponse = "Previous answer"
        };
        _context.ConversationExchanges.Add(exchange);
        await _context.SaveChangesAsync();

        var encryptedKey = _encryptionService.Encrypt("test-api-key");
        var apiKey = new ApiKey
        {
            UserId = user.Id,
            Provider = "claude",
            EncryptedKey = encryptedKey,
            IsActive = true
        };
        _context.ApiKeys.Add(apiKey);
        await _context.SaveChangesAsync();

        var mockResponse = new
        {
            content = new[] { new { text = "AI response with context" } },
            usage = new { input_tokens = 50, output_tokens = 30 }
        };

        var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(mockResponse))
            });

        var httpClient = new HttpClient(httpMessageHandlerMock.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act
        var result = await _responseService.GenerateResponseAsync(
            "New question",
            user.Id,
            conversationId: conversation.Id,
            aiProvider: "claude");

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Response);
    }

    [Fact]
    public async Task GenerateResponseAsync_WithDifferentStyles_AppliesStyleModifier()
    {
        // Arrange
        var user = new User { Email = "test@example.com", PasswordHash = "hash" };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var encryptedKey = _encryptionService.Encrypt("test-api-key");
        var apiKey = new ApiKey
        {
            UserId = user.Id,
            Provider = "claude",
            EncryptedKey = encryptedKey,
            IsActive = true
        };
        _context.ApiKeys.Add(apiKey);
        await _context.SaveChangesAsync();

        var mockResponse = new
        {
            content = new[] { new { text = "Casual response" } },
            usage = new { input_tokens = 10, output_tokens = 15 }
        };

        var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(mockResponse))
            });

        var httpClient = new HttpClient(httpMessageHandlerMock.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act
        var result = await _responseService.GenerateResponseAsync(
            "Test",
            user.Id,
            responseStyle: "casual",
            aiProvider: "claude");

        // Assert
        Assert.True(result.Success);
        Assert.Equal("casual", result.Style);
    }

    [Fact]
    public async Task GenerateResponseAsync_LogsTransaction()
    {
        // Arrange
        var user = new User { Email = "test@example.com", PasswordHash = "hash" };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var encryptedKey = _encryptionService.Encrypt("test-api-key");
        var apiKey = new ApiKey
        {
            UserId = user.Id,
            Provider = "claude",
            EncryptedKey = encryptedKey,
            IsActive = true
        };
        _context.ApiKeys.Add(apiKey);
        await _context.SaveChangesAsync();

        var mockResponse = new
        {
            content = new[] { new { text = "Response" } },
            usage = new { input_tokens = 10, output_tokens = 10 }
        };

        var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(mockResponse))
            });

        var httpClient = new HttpClient(httpMessageHandlerMock.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act
        await _responseService.GenerateResponseAsync(
            "Test",
            user.Id,
            aiProvider: "claude");

        // Assert
        var transaction = await _context.TransactionLogs
            .FirstOrDefaultAsync(t => t.UserId == user.Id && t.TransactionType == "ai_response");
        Assert.NotNull(transaction);
        Assert.Equal("claude", transaction.Provider);
        Assert.Equal(20, transaction.TokensUsed);
    }

    [Fact]
    public async Task GenerateResponseAsync_WithInvalidProvider_ReturnsError()
    {
        // Arrange
        var user = new User { Email = "test@example.com", PasswordHash = "hash" };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _responseService.GenerateResponseAsync(
            "Test",
            user.Id,
            aiProvider: "invalid-provider");

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task GenerateResponseAsync_WithUserPreferences_UsesPreferredStyle()
    {
        // Arrange
        var user = new User { Email = "test@example.com", PasswordHash = "hash" };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var preferences = new UserPreferences
        {
            UserId = user.Id,
            PreferredResponseStyle = "technical",
            PreferredAIProvider = "claude"
        };
        _context.UserPreferences.Add(preferences);

        var encryptedKey = _encryptionService.Encrypt("test-api-key");
        var apiKey = new ApiKey
        {
            UserId = user.Id,
            Provider = "claude",
            EncryptedKey = encryptedKey,
            IsActive = true
        };
        _context.ApiKeys.Add(apiKey);
        await _context.SaveChangesAsync();

        var mockResponse = new
        {
            content = new[] { new { text = "Technical response" } },
            usage = new { input_tokens = 10, output_tokens = 20 }
        };

        var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(mockResponse))
            });

        var httpClient = new HttpClient(httpMessageHandlerMock.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act
        var result = await _responseService.GenerateResponseAsync(
            "Test technical question",
            user.Id);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("technical", result.Style);
    }
}
