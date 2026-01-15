namespace AudioAssistant.Api.Services;

/// <summary>
/// Service for translating text between languages
/// </summary>
public interface ITranslationService
{
    /// <summary>
    /// Translate text from source language to target language
    /// </summary>
    Task<TranslationResult> TranslateAsync(
        string text,
        string targetLanguage,
        int userId,
        string? sourceLanguage = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Detect the language of the provided text
    /// </summary>
    Task<LanguageDetectionResult> DetectLanguageAsync(
        string text,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get list of supported languages
    /// </summary>
    Task<IEnumerable<LanguageInfo>> GetSupportedLanguagesAsync();

    /// <summary>
    /// Get translation history for a transcript
    /// </summary>
    Task<IEnumerable<Models.Translation>> GetTranslationHistoryAsync(
        int transcriptId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a translation operation
/// </summary>
public class TranslationResult
{
    public string OriginalText { get; set; } = string.Empty;
    public string TranslatedText { get; set; } = string.Empty;
    public string SourceLanguage { get; set; } = string.Empty;
    public string TargetLanguage { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Result of language detection
/// </summary>
public class LanguageDetectionResult
{
    public string DetectedLanguage { get; set; } = string.Empty;
    public string LanguageName { get; set; } = string.Empty;
    public float Confidence { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Information about a supported language
/// </summary>
public class LanguageInfo
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string NativeName { get; set; } = string.Empty;
}
