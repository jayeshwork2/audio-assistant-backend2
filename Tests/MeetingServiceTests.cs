using AudioAssistant.Api.Data;
using AudioAssistant.Api.Models;
using AudioAssistant.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AudioAssistant.Tests;

public class MeetingServiceTests
{
    private readonly AudioAssistantDbContext _context;
    private readonly Mock<ILogger<MeetingService>> _loggerMock;
    private readonly MeetingService _meetingService;

    public MeetingServiceTests()
    {
        var options = new DbContextOptionsBuilder<AudioAssistantDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AudioAssistantDbContext(options);
        _loggerMock = new Mock<ILogger<MeetingService>>();
        _meetingService = new MeetingService(_context, _loggerMock.Object);
    }

    [Fact]
    public async Task DetectMeetingTypeAsync_WithInterviewKeywords_ReturnsInterview()
    {
        // Arrange
        var transcript = "We are interviewing candidates for the software engineer position. What is your experience?";

        // Act
        var meetingType = await _meetingService.DetectMeetingTypeAsync(transcript);

        // Assert
        Assert.Equal("interview", meetingType);
    }

    [Fact]
    public async Task DetectMeetingTypeAsync_WithSalesKeywords_ReturnsSales()
    {
        // Arrange
        var transcript = "Let me show you our product demo and discuss pricing for your team.";

        // Act
        var meetingType = await _meetingService.DetectMeetingTypeAsync(transcript);

        // Assert
        Assert.Equal("sales", meetingType);
    }

    [Fact]
    public async Task DetectDomainAsync_WithTechnicalKeywords_ReturnsTechnical()
    {
        // Arrange
        var transcript = "We need to fix the API bug in the database query. The server is returning errors.";

        // Act
        var domain = await _meetingService.DetectDomainAsync(transcript);

        // Assert
        Assert.Equal("technical", domain);
    }

    [Fact]
    public async Task DetectDomainAsync_WithBusinessKeywords_ReturnsBusiness()
    {
        // Arrange
        var transcript = "Our revenue growth and market strategy need to improve customer acquisition.";

        // Act
        var domain = await _meetingService.DetectDomainAsync(transcript);

        // Assert
        Assert.Equal("business", domain);
    }

    [Fact]
    public async Task DetectFormalityAsync_WithFormalText_ReturnsFormal()
    {
        // Arrange
        var transcript = "Hereby, we are pleased to inform you regarding the upcoming proceedings. Kindly review the documentation.";

        // Act
        var formality = await _meetingService.DetectFormalityAsync(transcript);

        // Assert
        Assert.Equal("formal", formality);
    }

    [Fact]
    public async Task DetectFormalityAsync_WithInformalText_ReturnsInformal()
    {
        // Arrange
        var transcript = "Hey guys, yeah this is cool. Wanna grab some coffee later? BTW thanks!";

        // Act
        var formality = await _meetingService.DetectFormalityAsync(transcript);

        // Assert
        Assert.Equal("informal", formality);
    }

    [Fact]
    public async Task DetectUrgencyAsync_WithUrgentKeywords_ReturnsHigh()
    {
        // Arrange
        var transcript = "This is urgent! We must fix this immediately. It's critical and the deadline is today!";

        // Act
        var urgency = await _meetingService.DetectUrgencyAsync(transcript);

        // Assert
        Assert.Equal("high", urgency);
    }

    [Fact]
    public async Task DetectUrgencyAsync_WithNoUrgency_ReturnsLow()
    {
        // Arrange
        var transcript = "We should consider this option when we have time next month.";

        // Act
        var urgency = await _meetingService.DetectUrgencyAsync(transcript);

        // Assert
        Assert.Equal("low", urgency);
    }

    [Fact]
    public async Task ExtractActionItemsAsync_ExtractsActionItems()
    {
        // Arrange
        var transcript = "We need to update the documentation. John will send the report by Friday. We must review the code before deployment.";

        // Act
        var actionItems = await _meetingService.ExtractActionItemsAsync(transcript);

        // Assert
        Assert.NotEmpty(actionItems);
        Assert.Contains(actionItems, item => item.Contains("update"));
    }

    [Fact]
    public async Task ExtractKeyPointsAsync_ExtractsKeyPoints()
    {
        // Arrange
        var transcript = "The main point is that we decided to launch next month. It's important to note that budget is approved. The key issue is timeline management.";

        // Act
        var keyPoints = await _meetingService.ExtractKeyPointsAsync(transcript);

        // Assert
        Assert.NotEmpty(keyPoints);
    }

    [Fact]
    public async Task CreateMeetingAsync_CreatesMeeting()
    {
        // Arrange
        var user = new User { Email = "test@example.com", PasswordHash = "hash" };
        _context.Users.Add(user);
        var conversation = new Conversation
        {
            UserId = user.Id,
            SessionId = Guid.NewGuid().ToString(),
            MeetingType = "meeting",
            Domain = "business"
        };
        _context.Conversations.Add(conversation);
        await _context.SaveChangesAsync();

        // Act
        var meeting = await _meetingService.CreateMeetingAsync(conversation.Id, "Test Meeting");

        // Assert
        Assert.NotNull(meeting);
        Assert.Equal("Test Meeting", meeting.Title);
        Assert.Equal(conversation.Id, meeting.ConversationId);
    }

    [Fact]
    public async Task GenerateMeetingNotesAsync_GeneratesNotes()
    {
        // Arrange
        var user = new User { Email = "test@example.com", PasswordHash = "hash" };
        _context.Users.Add(user);
        var conversation = new Conversation
        {
            UserId = user.Id,
            SessionId = Guid.NewGuid().ToString()
        };
        _context.Conversations.Add(conversation);
        await _context.SaveChangesAsync();

        var meeting = await _meetingService.CreateMeetingAsync(conversation.Id, "Test Meeting");

        var transcript = new Transcript
        {
            ConversationId = conversation.Id,
            Text = "We decided to launch the product next month. Action item: prepare marketing materials.",
            Language = "en"
        };
        _context.Transcripts.Add(transcript);
        await _context.SaveChangesAsync();

        // Act
        var notes = await _meetingService.GenerateMeetingNotesAsync(meeting.Id);

        // Assert
        Assert.NotNull(notes);
        Assert.NotNull(notes.Summary);
        Assert.NotNull(notes.ActionItems);
        Assert.NotNull(notes.KeyPoints);
    }

    [Fact]
    public async Task GetMeetingAsync_ReturnsMeetingWithNotes()
    {
        // Arrange
        var user = new User { Email = "test@example.com", PasswordHash = "hash" };
        _context.Users.Add(user);
        var conversation = new Conversation
        {
            UserId = user.Id,
            SessionId = Guid.NewGuid().ToString()
        };
        _context.Conversations.Add(conversation);
        await _context.SaveChangesAsync();

        var meeting = await _meetingService.CreateMeetingAsync(conversation.Id, "Test Meeting");

        // Act
        var retrieved = await _meetingService.GetMeetingAsync(meeting.Id);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(meeting.Id, retrieved.Id);
    }
}
