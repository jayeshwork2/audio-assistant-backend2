using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AudioAssistant.Api.Models;

/// <summary>
/// Stores exported meeting notes
/// </summary>
public class Export
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    //[Required]
    public int MeetingNotesId { get; set; }

    [Required]
    [MaxLength(50)]
    public string Format { get; set; } = string.Empty;

    //[Required]
    [MaxLength(500)]
    public string FilePath { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ExpiresAt { get; set; }

    // Navigation property
    [ForeignKey(nameof(MeetingNotesId))]
    public MeetingNotes MeetingNotes { get; set; } = null!;
}
