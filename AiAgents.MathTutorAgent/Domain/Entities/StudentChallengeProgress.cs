namespace AiAgents.MathTutorAgent.Domain.Entities;

public class StudentChallengeProgress
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string ChallengeKey { get; set; } = string.Empty;
    public DateTime CompletedAtUtc { get; set; } = DateTime.UtcNow;

    public Student Student { get; set; } = null!;
}
