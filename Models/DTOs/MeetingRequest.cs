using System.ComponentModel.DataAnnotations;

namespace AudioAssistant.Api.Models.DTOs;

public class MeetingRequest
{
    [Required]
    public string ConversationId { get; set; } = string.Empty;

    [Required]
    public string Title { get; set; } = string.Empty;
}

public class MeetingDetectRequest
{
    [Required]
    public string Transcript { get; set; } = string.Empty;
}
