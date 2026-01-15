using AudioAssistant.Api.Data;
using AudioAssistant.Api.Models;
using AudioAssistant.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AudioAssistant.Tests;

public class TranslationServiceTests
{
    private readonly AudioAssistantDbContext _context;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<ILogger<TranslationService>> _loggerMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly TranslationService _translationService;

    public TranslationServiceTests()
    {
        var options = new DbContextOptionsBuilder<AudioAssistantDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AudioAssistantDbContext(options);
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _loggerMock = new Mock<ILogger<TranslationService>>();
        _configurationMock = new Mock<IConfiguration>();

        _translationService = new TranslationService(
            _context,
            _httpClientFactoryMock.Object,
            _loggerMock.Object,
            _configurationMock.Object);
    }

    [Fact]
    public async Task GetSupportedLanguagesAsync_ReturnsAllLanguages()
    {
        // Act
        var languages = await _translationService.GetSupportedLanguagesAsync();

        // Assert
        var languageList = languages.ToList();
        Assert.NotEmpty(languageList);
        Assert.True(languageList.Count >= 50);
        Assert.Contains(languageList, l => l.Code == "en");
        Assert.Contains(languageList, l => l.Code == "es");
        Assert.Contains(languageList, l => l.Code == "fr");
    }

    [Fact]
    public async Task DetectLanguageAsync_WithEnglishText_ReturnsEnglish()
    {
        // Arrange
        var text = "Hello, this is an English sentence.";

        // Act
        var result = await _translationService.DetectLanguageAsync(text);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("en", result.DetectedLanguage);
    }

    [Fact]
    public async Task TranslateAsync_SameSourceAndTarget_ReturnsOriginal()
    {
        // Arrange
        var user = new User { Email = "test@example.com", PasswordHash = "hash" };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var text = "Hello world";

        // Act
        var result = await _translationService.TranslateAsync(text, "en", user.Id, "en");

        // Assert
        Assert.True(result.Success);
        Assert.Equal(text, result.TranslatedText);
        Assert.Equal("en", result.SourceLanguage);
        Assert.Equal("en", result.TargetLanguage);
    }

    [Fact]
    public async Task TranslateAsync_CachesTranslation()
    {
        // Arrange
        var user = new User { Email = "test@example.com", PasswordHash = "hash" };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var text = "Hello";

        // Mock translation (will use mock translation since no API key)
        var result1 = await _translationService.TranslateAsync(text, "es", user.Id, "en");

        // Act - second call should use cache
        var result2 = await _translationService.TranslateAsync(text, "es", user.Id, "en");

        // Assert
        Assert.True(result1.Success);
        Assert.True(result2.Success);
        
        var cached = await _context.Translations
            .Where(t => t.UserId == user.Id && t.OriginalText == text)
            .ToListAsync();
        Assert.NotEmpty(cached);
    }

    [Fact]
    public async Task GetTranslationHistoryAsync_ReturnsHistory()
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
        var transcript = new Transcript
        {
            ConversationId = conversation.Id,
            Text = "Test",
            Language = "en"
        };
        _context.Transcripts.Add(transcript);
        await _context.SaveChangesAsync();

        var translation = new Translation
        {
            UserId = user.Id,
            TranscriptId = transcript.Id,
            OriginalText = "Hello",
            TranslatedText = "Hola",
            SourceLanguage = "en",
            TargetLanguage = "es"
        };
        _context.Translations.Add(translation);
        await _context.SaveChangesAsync();

        // Act
        var history = await _translationService.GetTranslationHistoryAsync(transcript.Id);

        // Assert
        var historyList = history.ToList();
        Assert.Single(historyList);
        Assert.Equal("Hello", historyList[0].OriginalText);
        Assert.Equal("Hola", historyList[0].TranslatedText);
    }

    [Fact]
    public async Task DetectLanguageAsync_WithCyrillicText_ReturnsRussian()
    {
        // Arrange
        var text = "Привет мир";

        // Act
        var result = await _translationService.DetectLanguageAsync(text);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("ru", result.DetectedLanguage);
    }

    [Fact]
    public async Task DetectLanguageAsync_WithChineseText_ReturnsChinese()
    {
        // Arrange
        var text = "你好世界";

        // Act
        var result = await _translationService.DetectLanguageAsync(text);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("zh", result.DetectedLanguage);
    }

    [Fact]
    public async Task TranslateAsync_WithAutoDetect_DetectsLanguage()
    {
        // Arrange
        var user = new User { Email = "test@example.com", PasswordHash = "hash" };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var text = "Hello world";

        // Act
        var result = await _translationService.TranslateAsync(text, "es", user.Id, "auto");

        // Assert
        Assert.True(result.Success);
        Assert.Equal("en", result.SourceLanguage);
    }
}
