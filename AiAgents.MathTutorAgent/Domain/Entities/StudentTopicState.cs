namespace AiAgents.MathTutorAgent.Domain.Entities;

public class StudentTopicState
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public int TopicId { get; set; }
    
    public float MasteryScore { get; set; } // 0-100
    public double Confidence { get; set; }   // 0-1
    public double ForgettingRisk { get; set; } // 0-1
    public DateTime LastPracticedUtc { get; set; }
    
    // Navigation
    public Student Student { get; set; } = null!;
    public Topic Topic { get; set; } = null!;
}