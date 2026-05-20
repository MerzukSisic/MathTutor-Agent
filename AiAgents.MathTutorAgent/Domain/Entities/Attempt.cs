namespace AiAgents.MathTutorAgent.Domain.Entities;

public class Attempt
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public int QuestionId { get; set; }
    public bool IsCorrect { get; set; }
    public int TimeMs { get; set; }
    public string AnswerRaw { get; set; } = string.Empty;
    public List<string> ErrorTagsDetected { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Student Student { get; set; } = null!;
    public Question Question { get; set; } = null!;
}