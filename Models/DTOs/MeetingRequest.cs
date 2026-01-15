using System.ComponentModel.DataAnnotations;

namespace AudioAssistant.Api.Models.DTOs;

public class MeetingRequest
{
    [Required]
    public int ConversationId { get; set; }

    [Required]
    public string Title { get; set; } = string.Empty;
}

public class MeetingDetectRequest
{
    [Required]
    public string Transcript { get; set; } = string.Empty;
}
