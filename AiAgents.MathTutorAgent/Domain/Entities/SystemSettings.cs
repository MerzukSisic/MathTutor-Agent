namespace AiAgents.MathTutorAgent.Domain.Entities;

public class SystemSettings
{
    public int Id { get; set; }
    public double MasteryAdvanceThreshold { get; set; } = 85.0;
    public double MasteryReviewThreshold { get; set; } = 60.0;
    public double PrerequisiteLockThreshold { get; set; } = 75.0;
    public int RevisionIntervalDays { get; set; } = 7;
    public double ForgettingRiskThreshold { get; set; } = 0.6;
}