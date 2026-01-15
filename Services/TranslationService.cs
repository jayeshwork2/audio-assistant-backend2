using AudioAssistant.Api.Data;
using AudioAssistant.Api.Models;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

namespace AudioAssistant.Api.Services;

public class TranslationService : ITranslationService
{
    private readonly AudioAssistantDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TranslationService> _logger;
    private readonly IConfiguration _configuration;

    private static readonly Dictionary<string, LanguageInfo> _supportedLanguages = new()
    {
        ["en"] = new() { Code = "en", Name = "English", NativeName = "English" },
        ["es"] = new() { Code = "es", Name = "Spanish", NativeName = "Español" },
        ["fr"] = new() { Code = "fr", Name = "French", NativeName = "Français" },
        ["de"] = new() { Code = "de", Name = "German", NativeName = "Deutsch" },
        ["it"] = new() { Code = "it", Name = "Italian", NativeName = "Italiano" },
        ["pt"] = new() { Code = "pt", Name = "Portuguese", NativeName = "Português" },
        ["ru"] = new() { Code = "ru", Name = "Russian", NativeName = "Русский" },
        ["ja"] = new() { Code = "ja", Name = "Japanese", NativeName = "日本語" },
        ["ko"] = new() { Code = "ko", Name = "Korean", NativeName = "한국어" },
        ["zh"] = new() { Code = "zh", Name = "Chinese", NativeName = "中文" },
        ["ar"] = new() { Code = "ar", Name = "Arabic", NativeName = "العربية" },
        ["hi"] = new() { Code = "hi", Name = "Hindi", NativeName = "हिन्दी" },
        ["bn"] = new() { Code = "bn", Name = "Bengali", NativeName = "বাংলা" },
        ["pa"] = new() { Code = "pa", Name = "Punjabi", NativeName = "ਪੰਜਾਬੀ" },
        ["te"] = new() { Code = "te", Name = "Telugu", NativeName = "తెలుగు" },
        ["mr"] = new() { Code = "mr", Name = "Marathi", NativeName = "मराठी" },
        ["ta"] = new() { Code = "ta", Name = "Tamil", NativeName = "தமிழ்" },
        ["tr"] = new() { Code = "tr", Name = "Turkish", NativeName = "Türkçe" },
        ["vi"] = new() { Code = "vi", Name = "Vietnamese", NativeName = "Tiếng Việt" },
        ["pl"] = new() { Code = "pl", Name = "Polish", NativeName = "Polski" },
        ["uk"] = new() { Code = "uk", Name = "Ukrainian", NativeName = "Українська" },
        ["nl"] = new() { Code = "nl", Name = "Dutch", NativeName = "Nederlands" },
        ["ro"] = new() { Code = "ro", Name = "Romanian", NativeName = "Română" },
        ["el"] = new() { Code = "el", Name = "Greek", NativeName = "Ελληνικά" },
        ["cs"] = new() { Code = "cs", Name = "Czech", NativeName = "Čeština" },
        ["sv"] = new() { Code = "sv", Name = "Swedish", NativeName = "Svenska" },
        ["hu"] = new() { Code = "hu", Name = "Hungarian", NativeName = "Magyar" },
        ["fi"] = new() { Code = "fi", Name = "Finnish", NativeName = "Suomi" },
        ["no"] = new() { Code = "no", Name = "Norwegian", NativeName = "Norsk" },
        ["da"] = new() { Code = "da", Name = "Danish", NativeName = "Dansk" },
        ["th"] = new() { Code = "th", Name = "Thai", NativeName = "ไทย" },
        ["id"] = new() { Code = "id", Name = "Indonesian", NativeName = "Bahasa Indonesia" },
        ["ms"] = new() { Code = "ms", Name = "Malay", NativeName = "Bahasa Melayu" },
        ["he"] = new() { Code = "he", Name = "Hebrew", NativeName = "עברית" },
        ["fa"] = new() { Code = "fa", Name = "Persian", NativeName = "فارسی" },
        ["ur"] = new() { Code = "ur", Name = "Urdu", NativeName = "اردو" },
        ["sw"] = new() { Code = "sw", Name = "Swahili", NativeName = "Kiswahili" },
        ["af"] = new() { Code = "af", Name = "Afrikaans", NativeName = "Afrikaans" },
        ["sq"] = new() { Code = "sq", Name = "Albanian", NativeName = "Shqip" },
        ["am"] = new() { Code = "am", Name = "Amharic", NativeName = "አማርኛ" },
        ["az"] = new() { Code = "az", Name = "Azerbaijani", NativeName = "Azərbaycan" },
        ["eu"] = new() { Code = "eu", Name = "Basque", NativeName = "Euskara" },
        ["be"] = new() { Code = "be", Name = "Belarusian", NativeName = "Беларуская" },
        ["bg"] = new() { Code = "bg", Name = "Bulgarian", NativeName = "Български" },
        ["ca"] = new() { Code = "ca", Name = "Catalan", NativeName = "Català" },
        ["hr"] = new() { Code = "hr", Name = "Croatian", NativeName = "Hrvatski" },
        ["et"] = new() { Code = "et", Name = "Estonian", NativeName = "Eesti" },
        ["gl"] = new() { Code = "gl", Name = "Galician", NativeName = "Galego" },
        ["ka"] = new() { Code = "ka", Name = "Georgian", NativeName = "ქართული" },
        ["gu"] = new() { Code = "gu", Name = "Gujarati", NativeName = "ગુજરાતી" },
        ["kn"] = new() { Code = "kn", Name = "Kannada", NativeName = "ಕನ್ನಡ" },
        ["lv"] = new() { Code = "lv", Name = "Latvian", NativeName = "Latviešu" },
        ["lt"] = new() { Code = "lt", Name = "Lithuanian", NativeName = "Lietuvių" },
        ["mk"] = new() { Code = "mk", Name = "Macedonian", NativeName = "Македонски" },
        ["ml"] = new() { Code = "ml", Name = "Malayalam", NativeName = "മലയാളം" },
        ["ne"] = new() { Code = "ne", Name = "Nepali", NativeName = "नेपाली" },
        ["sk"] = new() { Code = "sk", Name = "Slovak", NativeName = "Slovenčina" },
        ["sl"] = new() { Code = "sl", Name = "Slovenian", NativeName = "Slovenščina" },
        ["sr"] = new() { Code = "sr", Name = "Serbian", NativeName = "Српски" }
    };

    public TranslationService(
        AudioAssistantDbContext context,
        IHttpClientFactory httpClientFactory,
        ILogger<TranslationService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<TranslationResult> TranslateAsync(
        string text,
        string targetLanguage,
        int userId,
        string? sourceLanguage = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Detect source language if not provided
            if (string.IsNullOrEmpty(sourceLanguage) || sourceLanguage == "auto")
            {
                var detection = await DetectLanguageAsync(text, cancellationToken);
                if (detection.Success)
                {
                    sourceLanguage = detection.DetectedLanguage;
                }
                else
                {
                    sourceLanguage = "en"; // Default to English
                }
            }

            // If source and target are the same, return original text
            if (sourceLanguage.ToLower() == targetLanguage.ToLower())
            {
                return new TranslationResult
                {
                    OriginalText = text,
                    TranslatedText = text,
                    SourceLanguage = sourceLanguage,
                    TargetLanguage = targetLanguage,
                    Success = true
                };
            }

            // Check cache first
            var cached = await GetCachedTranslationAsync(text, sourceLanguage, targetLanguage, cancellationToken);
            if (cached != null)
            {
                return cached;
            }

            // Perform translation using Google Translate API
            var translatedText = await TranslateWithGoogleAsync(text, sourceLanguage, targetLanguage, cancellationToken);

            if (translatedText == null)
            {
                return new TranslationResult
                {
                    Success = false,
                    ErrorMessage = "Translation failed"
                };
            }

            var result = new TranslationResult
            {
                OriginalText = text,
                TranslatedText = translatedText,
                SourceLanguage = sourceLanguage,
                TargetLanguage = targetLanguage,
                Success = true
            };

            // Cache the translation
            await CacheTranslationAsync(userId, result, cancellationToken);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error translating text for user {UserId}", userId);
            return new TranslationResult
            {
                Success = false,
                ErrorMessage = $"Translation error: {ex.Message}"
            };
        }
    }

    public async Task<LanguageDetectionResult> DetectLanguageAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var apiKey = _configuration["GoogleTranslate:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                // Fallback: simple heuristic-based detection
                return DetectLanguageHeuristic(text);
            }

            var httpClient = _httpClientFactory.CreateClient();
            var url = $"https://translation.googleapis.com/language/translate/v2/detect?key={apiKey}";

            var requestBody = new { q = text };
            var response = await httpClient.PostAsync(
                url,
                new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"),
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return DetectLanguageHeuristic(text);
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var jsonDoc = JsonDocument.Parse(responseContent);

            var detectedLang = jsonDoc.RootElement
                .GetProperty("data")
                .GetProperty("detections")[0][0]
                .GetProperty("language").GetString() ?? "en";

            var confidence = jsonDoc.RootElement
                .GetProperty("data")
                .GetProperty("detections")[0][0]
                .GetProperty("confidence").GetSingle();

            var langName = _supportedLanguages.TryGetValue(detectedLang, out var info)
                ? info.Name
                : detectedLang;

            return new LanguageDetectionResult
            {
                DetectedLanguage = detectedLang,
                LanguageName = langName,
                Confidence = confidence,
                Success = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error detecting language, using heuristics");
            return DetectLanguageHeuristic(text);
        }
    }

    public Task<IEnumerable<LanguageInfo>> GetSupportedLanguagesAsync()
    {
        return Task.FromResult(_supportedLanguages.Values.OrderBy(l => l.Name).AsEnumerable());
    }

    public async Task<IEnumerable<Translation>> GetTranslationHistoryAsync(
        int transcriptId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Translations
            .Where(t => t.TranscriptId == transcriptId)
            .OrderByDescending(t => t.TranslatedAt)
            .ToListAsync(cancellationToken);
    }

    private async Task<string?> TranslateWithGoogleAsync(
        string text,
        string sourceLanguage,
        string targetLanguage,
        CancellationToken cancellationToken)
    {
        try
        {
            var apiKey = _configuration["GoogleTranslate:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogWarning("Google Translate API key not configured, using mock translation");
                return $"[Translated to {targetLanguage}]: {text}";
            }

            var httpClient = _httpClientFactory.CreateClient();
            var url = $"https://translation.googleapis.com/language/translate/v2?key={apiKey}";

            var requestBody = new
            {
                q = text,
                source = sourceLanguage,
                target = targetLanguage,
                format = "text"
            };

            var response = await httpClient.PostAsync(
                url,
                new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"),
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Google Translate API error: {Error}", error);
                return null;
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var jsonDoc = JsonDocument.Parse(responseContent);

            return jsonDoc.RootElement
                .GetProperty("data")
                .GetProperty("translations")[0]
                .GetProperty("translatedText").GetString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Google Translate API");
            return null;
        }
    }

    private LanguageDetectionResult DetectLanguageHeuristic(string text)
    {
        // Simple heuristic: check for common character ranges
        var hasLatin = text.Any(c => (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'));
        var hasCyrillic = text.Any(c => (c >= 0x0400 && c <= 0x04FF));
        var hasArabic = text.Any(c => (c >= 0x0600 && c <= 0x06FF));
        var hasCJK = text.Any(c => (c >= 0x4E00 && c <= 0x9FFF));
        var hasJapanese = text.Any(c => (c >= 0x3040 && c <= 0x309F) || (c >= 0x30A0 && c <= 0x30FF));
        var hasKorean = text.Any(c => (c >= 0xAC00 && c <= 0xD7AF));

        string detectedLang = "en";
        if (hasCyrillic) detectedLang = "ru";
        else if (hasArabic) detectedLang = "ar";
        else if (hasKorean) detectedLang = "ko";
        else if (hasJapanese) detectedLang = "ja";
        else if (hasCJK) detectedLang = "zh";

        var langName = _supportedLanguages.TryGetValue(detectedLang, out var info)
            ? info.Name
            : "English";

        return new LanguageDetectionResult
        {
            DetectedLanguage = detectedLang,
            LanguageName = langName,
            Confidence = 0.7f,
            Success = true
        };
    }

    private async Task<TranslationResult?> GetCachedTranslationAsync(
        string text,
        string sourceLanguage,
        string targetLanguage,
        CancellationToken cancellationToken)
    {
        var cached = await _context.Translations
            .Where(t => t.OriginalText == text &&
                       t.SourceLanguage == sourceLanguage &&
                       t.TargetLanguage == targetLanguage)
            .OrderByDescending(t => t.TranslatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (cached != null)
        {
            return new TranslationResult
            {
                OriginalText = cached.OriginalText,
                TranslatedText = cached.TranslatedText,
                SourceLanguage = cached.SourceLanguage,
                TargetLanguage = cached.TargetLanguage,
                Timestamp = cached.TranslatedAt,
                Success = true
            };
        }

        return null;
    }

    private async Task CacheTranslationAsync(
        int userId,
        TranslationResult result,
        CancellationToken cancellationToken)
    {
        try
        {
            var translation = new Translation
            {
                UserId = userId,
                OriginalText = result.OriginalText,
                TranslatedText = result.TranslatedText,
                SourceLanguage = result.SourceLanguage,
                TargetLanguage = result.TargetLanguage,
                TranslatedAt = DateTime.UtcNow
            };

            _context.Translations.Add(translation);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cache translation");
        }
    }
}
