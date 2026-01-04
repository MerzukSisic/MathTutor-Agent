namespace AiAgents.MathTutorAgent.Application.DTOs;

public class UploadImagePayloadDto
{
    public string ImageBase64 { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
}