using AudioAssistant.Api.Data;
using AudioAssistant.Api.Models;
using AudioAssistant.Api.Services;
using AudioAssistant.Api.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AudioAssistant.Tests;

/// <summary>
/// Integration tests to verify end-to-end workflow
/// </summary>
public class IntegrationTests
{
    private readonly AudioAssistantDbContext _context;
    private readonly EncryptionService _encryptionService;
    private readonly Mock<ILogger<ConversationService>> _conversationLoggerMock;
    private readonly Mock<ILogger<MeetingService>> _meetingLoggerMock;
    private readonly Mock<ILogger<ExportService>> _exportLoggerMock;
    private readonly ConversationService _conversationService;
    private readonly MeetingService _meetingService;
    private readonly ExportService _exportService;

    public IntegrationTests()
    {
        var options = new DbContextOptionsBuilder<AudioAssistantDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AudioAssistantDbContext(options);
        _encryptionService = new EncryptionService("test-encryption-key-minimum-32");
        _conversationLoggerMock = new Mock<ILogger<ConversationService>>();
        _meetingLoggerMock = new Mock<ILogger<MeetingService>>();
        _exportLoggerMock = new Mock<ILogger<ExportService>>();

        _conversationService = new ConversationService(_context, _conversationLoggerMock.Object);
        _meetingService = new MeetingService(_context, _meetingLoggerMock.Object);
        _exportService = new ExportService(_context, _exportLoggerMock.Object);
    }

    [Fact]
    public async Task FullWorkflow_CreateConversation_DetectMeeting_GenerateNotes_Export()
    {
        // Step 1: Create user
        var user = new User
        {
            Email = "integration@test.com",
            PasswordHash = "hash",
            IsActive = true
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Step 2: Create conversation
        var conversation = await _conversationService.CreateConversationAsync(
            user.Id,
            "interview",
            "technical");

        Assert.NotNull(conversation);
        Assert.Equal("interview", conversation.MeetingType);
        Assert.Equal("technical", conversation.Domain);

        // Step 3: Add exchanges
        var exchange1 = await _conversationService.AddExchangeAsync(
            conversation.Id,
            "Tell me about your experience with ASP.NET Core",
            "I have 5 years of experience building APIs with ASP.NET Core...",
            "formal",
            "claude");

        var exchange2 = await _conversationService.AddExchangeAsync(
            conversation.Id,
            "What is your biggest technical challenge?",
            "The most challenging project was implementing microservices...",
            "formal",
            "claude");

        Assert.NotNull(exchange1);
        Assert.NotNull(exchange2);
        Assert.Equal(1, exchange1.Sequence);
        Assert.Equal(2, exchange2.Sequence);

        // Step 4: Create meeting
        var meeting = await _meetingService.CreateMeetingAsync(
            conversation.Id,
            "Senior Developer Interview");

        Assert.NotNull(meeting);
        Assert.Equal("Senior Developer Interview", meeting.Title);

        // Step 5: Add transcript
        var transcript = new Transcript
        {
            ConversationId = conversation.Id,
            Text = "We discussed the candidate's experience with ASP.NET Core, microservices, and their technical challenges. " +
                   "The candidate demonstrated strong technical skills. Action item: follow up with references.",
            Language = "en",
            Provider = "GroqWhisper"
        };
        _context.Transcripts.Add(transcript);
        await _context.SaveChangesAsync();

        // Step 6: Detect meeting characteristics
        var meetingType = await _meetingService.DetectMeetingTypeAsync(transcript.Text);
        var domain = await _meetingService.DetectDomainAsync(transcript.Text);
        var formality = await _meetingService.DetectFormalityAsync(transcript.Text);
        var urgency = await _meetingService.DetectUrgencyAsync(transcript.Text);

        Assert.Equal("interview", meetingType);
        Assert.Equal("technical", domain);
        Assert.Contains(new[] { "formal", "mixed" }, f => f == formality);
        Assert.Contains(new[] { "low", "medium" }, u => u == urgency);

        // Step 7: Extract action items and key points
        var actionItems = await _meetingService.ExtractActionItemsAsync(transcript.Text);
        var keyPoints = await _meetingService.ExtractKeyPointsAsync(transcript.Text);

        Assert.NotEmpty(actionItems);
        Assert.NotEmpty(keyPoints);

        // Step 8: Generate meeting notes
        var notes = await _meetingService.GenerateMeetingNotesAsync(meeting.Id);

        Assert.NotNull(notes);
        Assert.NotNull(notes.Summary);
        Assert.NotNull(notes.ActionItems);
        Assert.NotNull(notes.KeyPoints);

        // Step 9: Export as Markdown
        var markdownExport = await _exportService.ExportAsMarkdownAsync(meeting.Id, user.Id);

        Assert.True(markdownExport.Success);
        Assert.NotEmpty(markdownExport.Content);
        Assert.Contains("# Senior Developer Interview", markdownExport.Content);
        Assert.Contains("## Summary", markdownExport.Content);

        // Step 10: Export as PDF (HTML)
        var pdfExport = await _exportService.ExportAsPdfAsync(meeting.Id, user.Id);

        Assert.True(pdfExport.Success);
        Assert.NotEmpty(pdfExport.Content);
        Assert.Contains("<html>", pdfExport.Content);
        Assert.Contains("Senior Developer Interview", pdfExport.Content);

        // Step 11: Export as Plain Text
        var textExport = await _exportService.ExportAsPlainTextAsync(meeting.Id, user.Id);

        Assert.True(textExport.Success);
        Assert.NotEmpty(textExport.Content);
        Assert.Contains("SENIOR DEVELOPER INTERVIEW", textExport.Content);

        // Step 12: Verify export history
        var exportHistory = await _exportService.GetExportHistoryAsync(user.Id);
        var historyList = exportHistory.ToList();

        Assert.Equal(3, historyList.Count);
        Assert.Contains(historyList, e => e.Format == "markdown");
        Assert.Contains(historyList, e => e.Format == "pdf");
        Assert.Contains(historyList, e => e.Format == "text");

        // Step 13: Get aggregated context
        var context = await _conversationService.GetAggregatedContextAsync(conversation.Id);

        Assert.NotNull(context);
        Assert.Contains("interview", context);
        Assert.Contains("technical", context);
        Assert.Contains("Tell me about your experience", context);

        // Step 14: End conversation
        await _conversationService.EndConversationAsync(conversation.Id);

        var endedConversation = await _conversationService.GetConversationAsync(conversation.Id);
        Assert.NotNull(endedConversation!.EndedAt);
        Assert.True(endedConversation.Duration >= 0);
    }

    [Fact]
    public async Task MultiLanguage_Workflow_TranslationCaching()
    {
        // This test would verify translation functionality
        // Since we don't have the translation service properly mocked with HTTP client,
        // we'll just verify the data layer
        
        var user = new User
        {
            Email = "multilang@test.com",
            PasswordHash = "hash"
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Add a translation manually
        var translation = new Translation
        {
            UserId = user.Id,
            OriginalText = "Hello world",
            TranslatedText = "Hola mundo",
            SourceLanguage = "en",
            TargetLanguage = "es",
            TranslatedAt = DateTime.UtcNow
        };
        _context.Translations.Add(translation);
        await _context.SaveChangesAsync();

        // Verify it was stored
        var stored = await _context.Translations
            .FirstOrDefaultAsync(t => t.UserId == user.Id);

        Assert.NotNull(stored);
        Assert.Equal("Hello world", stored.OriginalText);
        Assert.Equal("Hola mundo", stored.TranslatedText);
        Assert.Equal("en", stored.SourceLanguage);
        Assert.Equal("es", stored.TargetLanguage);
    }

    [Fact]
    public async Task UserPreferences_Integration()
    {
        var user = new User
        {
            Email = "prefs@test.com",
            PasswordHash = "hash"
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var preferences = new UserPreferences
        {
            UserId = user.Id,
            PreferredLanguage = "en",
            PreferredAIProvider = "claude",
            PreferredSTTProvider = "GroqWhisper",
            PreferredResponseStyle = "technical",
            DefaultExportFormat = "markdown",
            DefaultMeetingType = "interview",
            DefaultDomain = "technical"
        };
        _context.UserPreferences.Add(preferences);
        await _context.SaveChangesAsync();

        // Verify preferences
        var stored = await _context.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == user.Id);

        Assert.NotNull(stored);
        Assert.Equal("technical", stored.PreferredResponseStyle);
        Assert.Equal("markdown", stored.DefaultExportFormat);
        Assert.Equal("claude", stored.PreferredAIProvider);
    }
}
