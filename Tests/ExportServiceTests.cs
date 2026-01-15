using AudioAssistant.Api.Data;
using AudioAssistant.Api.Models;
using AudioAssistant.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AudioAssistant.Tests;

public class ExportServiceTests
{
    private readonly AudioAssistantDbContext _context;
    private readonly Mock<ILogger<ExportService>> _loggerMock;
    private readonly ExportService _exportService;

    public ExportServiceTests()
    {
        var options = new DbContextOptionsBuilder<AudioAssistantDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AudioAssistantDbContext(options);
        _loggerMock = new Mock<ILogger<ExportService>>();
        _exportService = new ExportService(_context, _loggerMock.Object);
    }

    private async Task<Meeting> CreateTestMeeting()
    {
        var user = new User { Email = "test@example.com", PasswordHash = "hash" };
        _context.Users.Add(user);
        
        var conversation = new Conversation
        {
            UserId = user.Id,
            SessionId = Guid.NewGuid().ToString(),
            MeetingType = "meeting",
            Domain = "technical"
        };
        _context.Conversations.Add(conversation);
        await _context.SaveChangesAsync();

        var meeting = new Meeting
        {
            ConversationId = conversation.Id,
            Title = "Test Meeting",
            StartTime = DateTime.UtcNow,
            Type = "meeting",
            Domain = "technical"
        };
        _context.Meetings.Add(meeting);

        var notes = new MeetingNotes
        {
            MeetingId = meeting.Id,
            Summary = "This is a test meeting",
            KeyPoints = "Point 1\nPoint 2",
            ActionItems = "Action 1\nAction 2"
        };
        _context.MeetingNotes.Add(notes);

        var exchange = new ConversationExchange
        {
            ConversationId = conversation.Id,
            Sequence = 1,
            UserInput = "Test question",
            AiResponse = "Test answer"
        };
        _context.ConversationExchanges.Add(exchange);

        await _context.SaveChangesAsync();

        return meeting;
    }

    [Fact]
    public async Task ExportAsPdfAsync_GeneratesHtmlContent()
    {
        // Arrange
        var meeting = await CreateTestMeeting();
        var user = await _context.Users.FirstAsync();

        // Act
        var result = await _exportService.ExportAsPdfAsync(meeting.Id, user.Id);

        // Assert
        Assert.True(result.Success);
        Assert.NotEmpty(result.Content);
        Assert.Equal("html", result.Format);
        Assert.Equal("text/html", result.ContentType);
        Assert.Contains("Test Meeting", result.Content);
        Assert.Contains(".html", result.FileName);
    }

    [Fact]
    public async Task ExportAsMarkdownAsync_GeneratesMarkdownContent()
    {
        // Arrange
        var meeting = await CreateTestMeeting();
        var user = await _context.Users.FirstAsync();

        // Act
        var result = await _exportService.ExportAsMarkdownAsync(meeting.Id, user.Id);

        // Assert
        Assert.True(result.Success);
        Assert.NotEmpty(result.Content);
        Assert.Equal("markdown", result.Format);
        Assert.Equal("text/markdown", result.ContentType);
        Assert.Contains("# Test Meeting", result.Content);
        Assert.Contains("## Summary", result.Content);
        Assert.Contains(".md", result.FileName);
    }

    [Fact]
    public async Task ExportAsPlainTextAsync_GeneratesTextContent()
    {
        // Arrange
        var meeting = await CreateTestMeeting();
        var user = await _context.Users.FirstAsync();

        // Act
        var result = await _exportService.ExportAsPlainTextAsync(meeting.Id, user.Id);

        // Assert
        Assert.True(result.Success);
        Assert.NotEmpty(result.Content);
        Assert.Equal("text", result.Format);
        Assert.Equal("text/plain", result.ContentType);
        Assert.Contains("TEST MEETING", result.Content);
        Assert.Contains(".txt", result.FileName);
    }

    [Fact]
    public async Task ExportAsPdfAsync_LogsExport()
    {
        // Arrange
        var meeting = await CreateTestMeeting();
        var user = await _context.Users.FirstAsync();

        // Act
        await _exportService.ExportAsPdfAsync(meeting.Id, user.Id);

        // Assert
        var export = await _context.Exports
            .FirstOrDefaultAsync(e => e.UserId == user.Id && e.MeetingId == meeting.Id && e.Format == "pdf");
        Assert.NotNull(export);
        Assert.Equal("pdf", export.Format);
    }

    [Fact]
    public async Task GetExportHistoryAsync_ReturnsHistory()
    {
        // Arrange
        var meeting = await CreateTestMeeting();
        var user = await _context.Users.FirstAsync();

        await _exportService.ExportAsPdfAsync(meeting.Id, user.Id);
        await _exportService.ExportAsMarkdownAsync(meeting.Id, user.Id);

        // Act
        var history = await _exportService.GetExportHistoryAsync(user.Id);

        // Assert
        var historyList = history.ToList();
        Assert.Equal(2, historyList.Count);
    }

    [Fact]
    public async Task ExportAsPdfAsync_WithNonexistentMeeting_ReturnsError()
    {
        // Arrange
        var user = new User { Email = "test@example.com", PasswordHash = "hash" };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _exportService.ExportAsPdfAsync(999, user.Id);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task ExportAsMarkdownAsync_IncludesActionItems()
    {
        // Arrange
        var meeting = await CreateTestMeeting();
        var user = await _context.Users.FirstAsync();

        // Act
        var result = await _exportService.ExportAsMarkdownAsync(meeting.Id, user.Id);

        // Assert
        Assert.Contains("Action Items", result.Content);
        Assert.Contains("Action 1", result.Content);
        Assert.Contains("Action 2", result.Content);
    }

    [Fact]
    public async Task ExportAsPlainTextAsync_IncludesKeyPoints()
    {
        // Arrange
        var meeting = await CreateTestMeeting();
        var user = await _context.Users.FirstAsync();

        // Act
        var result = await _exportService.ExportAsPlainTextAsync(meeting.Id, user.Id);

        // Assert
        Assert.Contains("KEY POINTS", result.Content);
        Assert.Contains("Point 1", result.Content);
        Assert.Contains("Point 2", result.Content);
    }
}
