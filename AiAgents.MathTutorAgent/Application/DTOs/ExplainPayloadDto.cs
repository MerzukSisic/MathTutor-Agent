namespace AiAgents.MathTutorAgent.Application.DTOs;

public class ExplainPayloadDto
{
    public int? QuestionId { get; set; }
    public int? TopicId { get; set; }
    public string? ErrorTag { get; set; }
}