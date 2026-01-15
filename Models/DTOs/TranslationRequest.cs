using System.ComponentModel.DataAnnotations;

namespace AudioAssistant.Api.Models.DTOs;

public class TranslationRequest
{
    [Required]
    public string Text { get; set; } = string.Empty;

    [Required]
    public string TargetLanguage { get; set; } = string.Empty;

    public string? SourceLanguage { get; set; }
}

public class DetectLanguageRequest
{
    [Required]
    public string Text { get; set; } = string.Empty;
}
