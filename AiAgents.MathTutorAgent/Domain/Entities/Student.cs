namespace AiAgents.MathTutorAgent.Domain.Entities;

public class Student
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation
    public List<StudentTopicState> TopicStates { get; set; } = new();
    public List<Attempt> Attempts { get; set; } = new();
    public List<ImageNote> ImageNotes { get; set; } = new();
    public List<StudentChallengeProgress> ChallengeProgress { get; set; } = new();
}
