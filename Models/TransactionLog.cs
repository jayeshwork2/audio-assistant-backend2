using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AudioAssistant.Api.Models;

/// <summary>
/// Logs API transactions for monitoring and billing
/// </summary>
public class TransactionLog
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    //[Required]
    public int UserId { get; set; }

    [Required]
    [MaxLength(100)]
    public string TransactionType { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Provider { get; set; }

    //[Required]
    public string RequestData { get; set; } = string.Empty;

    //[Required]
    public string ResponseData { get; set; } = string.Empty;

    public int? TokensUsed { get; set; }

    //[Required]
    [MaxLength(50)]
    public string Status { get; set; } = string.Empty;
    public decimal? Cost { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    //[ForeignKey(nameof(UserId))]
    //public User User { get; set; } = null!;
}
