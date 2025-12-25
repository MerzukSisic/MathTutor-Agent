namespace AiAgents.MathTutorAgent.Application.DTOs;

public class ExplanationDto
{
    public string Explanation { get; set; } = string.Empty;
    public string? Example { get; set; }
    public List<ReferenceDto> Sources { get; set; } = new();
}