using System.ComponentModel.DataAnnotations;

namespace AudioAssistant.Api.Models.DTOs;

public class ExportRequest
{
    [Required]
    public int MeetingId { get; set; }

    public string? EmailAddress { get; set; }
}
