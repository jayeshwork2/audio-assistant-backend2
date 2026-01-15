using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AudioAssistant.Api.Models;

/// <summary>
/// Stores meeting notes and summaries
/// </summary>
public class MeetingNotes
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int MeetingId { get; set; }

    public string? FullTranscript { get; set; }

    public string? Summary { get; set; }

    public string? ActionItems { get; set; }

    public string? KeyPoints { get; set; }

    public string? SpeakerLabels { get; set; }

    public string? Decisions { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(MeetingId))]
    public Meeting Meeting { get; set; } = null!;

    public ICollection<Export> Exports { get; set; } = new List<Export>();
}
