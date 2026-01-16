using System.ComponentModel.DataAnnotations;

namespace AudioAssistant.Api.Models.DTOs;

public class ResponseRequest
{
    [Required]
    public string Transcript { get; set; } = string.Empty;

    public string? ConversationId { get; set; }

    public string? ResponseStyle { get; set; }

    public string? AiProvider { get; set; }

    public string? UsersApikey { get; set; }
}

public class ResponseStyleRequest
{
    [Required]
    public string Response { get; set; } = string.Empty;

    [Required]
    public string Style { get; set; } = string.Empty;
}

public class SaveStylePreferenceRequest
{
    [Required]
    public string PreferredStyle { get; set; } = string.Empty;
}
