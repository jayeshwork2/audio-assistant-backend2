using AudioAssistant.Api.Data;
using AudioAssistant.Api.Models;
using AudioAssistant.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AudioAssistant.Tests;

public class ConversationServiceTests
{
    private readonly AudioAssistantDbContext _context;
    private readonly Mock<ILogger<ConversationService>> _loggerMock;
    private readonly ConversationService _conversationService;

    public ConversationServiceTests()
    {
        var options = new DbContextOptionsBuilder<AudioAssistantDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AudioAssistantDbContext(options);
        _loggerMock = new Mock<ILogger<ConversationService>>();
        _conversationService = new ConversationService(_context, _loggerMock.Object);
    }

    [Fact]
    public async Task CreateConversationAsync_CreatesConversation()
    {
        // Arrange
        var user = new User { Email = "test@example.com", PasswordHash = "hash" };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var conversation = await _conversationService.CreateConversationAsync(
            user.Id,
            "meeting",
            "technical");

        // Assert
        Assert.NotNull(conversation);
        Assert.Equal(user.Id, conversation.UserId);
        Assert.Equal("meeting", conversation.MeetingType);
        Assert.Equal("technical", conversation.Domain);
        Assert.NotNull(conversation.SessionId);
    }

    [Fact]
    public async Task AddExchangeAsync_AddsExchange()
    {
        // Arrange
        var user = new User { Email = "test@example.com", PasswordHash = "hash" };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var conversation = await _conversationService.CreateConversationAsync(user.Id);

        // Act
        var exchange = await _conversationService.AddExchangeAsync(
            conversation.Id,
            "User question",
            "AI response",
            "formal",
            "claude");

        // Assert
        Assert.NotNull(exchange);
        Assert.Equal(1, exchange.Sequence);
        Assert.Equal("User question", exchange.UserInput);
        Assert.Equal("AI response", exchange.AiResponse);
        Assert.Equal("formal", exchange.ResponseStyle);
        Assert.Equal("claude", exchange.AiProvider);
    }

    [Fact]
    public async Task AddExchangeAsync_IncrementsSequence()
    {
        // Arrange
        var user = new User { Email = "test@example.com", PasswordHash = "hash" };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var conversation = await _conversationService.CreateConversationAsync(user.Id);

        await _conversationService.AddExchangeAsync(conversation.Id, "First", "Response1");
        
        // Act
        var exchange2 = await _conversationService.AddExchangeAsync(conversation.Id, "Second", "Response2");

        // Assert
        Assert.Equal(2, exchange2.Sequence);
    }

    [Fact]
    public async Task GetConversationAsync_ReturnsConversationWithExchanges()
    {
        // Arrange
        var user = new User { Email = "test@example.com", PasswordHash = "hash" };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var conversation = await _conversationService.CreateConversationAsync(user.Id);
        await _conversationService.AddExchangeAsync(conversation.Id, "Question", "Answer");

        // Act
        var retrieved = await _conversationService.GetConversationAsync(conversation.Id);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Single(retrieved.Exchanges);
    }

    [Fact]
    public async Task GetConversationHistoryAsync_ReturnsLimitedHistory()
    {
        // Arrange
        var user = new User { Email = "test@example.com", PasswordHash = "hash" };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var conversation = await _conversationService.CreateConversationAsync(user.Id);

        for (int i = 0; i < 30; i++)
        {
            await _conversationService.AddExchangeAsync(conversation.Id, $"Q{i}", $"A{i}");
        }

        // Act
        var history = await _conversationService.GetConversationHistoryAsync(conversation.Id, 10);

        // Assert
        var historyList = history.ToList();
        Assert.Equal(10, historyList.Count);
    }

    [Fact]
    public async Task UpdateContextAsync_UpdatesConversation()
    {
        // Arrange
        var user = new User { Email = "test@example.com", PasswordHash = "hash" };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var conversation = await _conversationService.CreateConversationAsync(user.Id);

        // Act
        await _conversationService.UpdateContextAsync(
            conversation.Id,
            "interview",
            "business",
            "Test summary");

        // Assert
        var updated = await _conversationService.GetConversationAsync(conversation.Id);
        Assert.Equal("interview", updated!.MeetingType);
        Assert.Equal("business", updated.Domain);
        Assert.Equal("Test summary", updated.Summary);
    }

    [Fact]
    public async Task EndConversationAsync_SetsEndTime()
    {
        // Arrange
        var user = new User { Email = "test@example.com", PasswordHash = "hash" };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var conversation = await _conversationService.CreateConversationAsync(user.Id);

        // Act
        await _conversationService.EndConversationAsync(conversation.Id);

        // Assert
        var ended = await _conversationService.GetConversationAsync(conversation.Id);
        Assert.NotNull(ended!.EndedAt);
        Assert.NotNull(ended.Duration);
        Assert.True(ended.Duration >= 0);
    }

    [Fact]
    public async Task GetAggregatedContextAsync_ReturnsFormattedContext()
    {
        // Arrange
        var user = new User { Email = "test@example.com", PasswordHash = "hash" };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var conversation = await _conversationService.CreateConversationAsync(user.Id, "meeting", "technical");
        await _conversationService.AddExchangeAsync(conversation.Id, "Question 1", "Answer 1");
        await _conversationService.AddExchangeAsync(conversation.Id, "Question 2", "Answer 2");

        // Act
        var context = await _conversationService.GetAggregatedContextAsync(conversation.Id);

        // Assert
        Assert.NotNull(context);
        Assert.Contains("meeting", context);
        Assert.Contains("technical", context);
        Assert.Contains("Question 1", context);
        Assert.Contains("Answer 1", context);
    }

    [Fact]
    public async Task AddExchangeAsync_WithInvalidConversation_ThrowsException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _conversationService.AddExchangeAsync(999, "Test", "Response"));
    }
}
