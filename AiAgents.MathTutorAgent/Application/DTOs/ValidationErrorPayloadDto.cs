namespace AiAgents.MathTutorAgent.Application.DTOs;

public sealed class ValidationErrorPayloadDto
{
    public string Error { get; set; } = string.Empty;
    public string[]? Errors { get; set; }
    public string? Details { get; set; }
}
