namespace AudioAssistant.Api.Models.DTOs;

public class MeetingDetectionResult
{
    public string MeetingType { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public string Formality { get; set; } = string.Empty;
    public string Urgency { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public List<string> KeyPoints { get; set; } = new();
    public List<string> ActionItems { get; set; } = new();
    public string? MeetingId { get; set; }
}
