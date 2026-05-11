namespace AiAgents.MathTutorAgent.Application.DTOs;

public class SubmitAnswerPayloadDto
{
    public int QuestionId { get; set; }
    public string Answer { get; set; } = string.Empty;
    public int TimeMs { get; set; }
    public bool TimedOut { get; set; }
}
