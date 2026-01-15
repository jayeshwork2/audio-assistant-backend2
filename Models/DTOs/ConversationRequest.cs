using System.ComponentModel.DataAnnotations;

namespace AudioAssistant.Api.Models.DTOs;

public class ConversationRequest
{
    public string? MeetingType { get; set; }
    public string? Domain { get; set; }
}

public class ExchangeRequest
{
    [Required]
    public string UserInput { get; set; } = string.Empty;

    public string? AiResponse { get; set; }
    public string? ResponseStyle { get; set; }
    public string? AiProvider { get; set; }
    public string? Context { get; set; }
}

public class UpdateConversationRequest
{
    public string? MeetingType { get; set; }
    public string? Domain { get; set; }
    public string? Summary { get; set; }
}
