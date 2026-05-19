namespace AiAgents.MathTutorAgent.Application.DTOs;

public sealed class AnswerEvaluationPayloadDto
{
    public bool IsCorrect { get; set; }
    public bool IsTimedOut { get; set; }
    public string CorrectAnswer { get; set; } = string.Empty;
    public double MasteryScore { get; set; }
    public string Decision { get; set; } = string.Empty;
    public int TimeLimitSeconds { get; set; }
    public double TimeSpentSeconds { get; set; }
    public CrossMathMilestoneDto? MilestoneChallenge { get; set; }
}
