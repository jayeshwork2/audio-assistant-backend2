using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AudioAssistant.Api.Models;

/// <summary>
/// Stores user preferences for AI interactions
/// </summary>
public class UserPreferences
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [MaxLength(10)]
    public string PreferredLanguage { get; set; } = "en";

    [MaxLength(50)]
    public string PreferredAIProvider { get; set; } = "groq";

    [MaxLength(50)]
    public string PreferredSTTProvider { get; set; } = "GroqWhisper";

    [MaxLength(50)]
    public string PreferredResponseStyle { get; set; } = "formal";

    [MaxLength(20)]
    public string DefaultExportFormat { get; set; } = "pdf";

    [MaxLength(50)]
    public string DefaultMeetingType { get; set; } = "general";

    [MaxLength(100)]
    public string DefaultDomain { get; set; } = "general";

    [MaxLength(50)]
    public string FormalityLevel { get; set; } = "professional";

    [MaxLength(10)]
    public string LastUsedLanguage { get; set; } = "en";

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;
}
