using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AudioAssistant.Api.Models;

/// <summary>
/// Stores translations of transcripts
/// </summary>
public class Translation
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    public int? TranscriptId { get; set; }

    [Required]
    public string OriginalText { get; set; } = string.Empty;

    [Required]
    public string TranslatedText { get; set; } = string.Empty;

    [Required]
    [MaxLength(10)]
    public string SourceLanguage { get; set; } = string.Empty;

    [Required]
    [MaxLength(10)]
    public string TargetLanguage { get; set; } = string.Empty;

    public DateTime TranslatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    [ForeignKey(nameof(TranscriptId))]
    public Transcript? Transcript { get; set; }
}
