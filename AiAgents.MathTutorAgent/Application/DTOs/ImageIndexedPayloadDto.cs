namespace AiAgents.MathTutorAgent.Application.DTOs;

public sealed class ImageIndexedPayloadDto
{
    public int ImageNoteId { get; set; }
    public string ExtractedText { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string[] Tags { get; set; } = [];
}
