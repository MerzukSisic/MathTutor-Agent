namespace AiAgents.MathTutorAgent.Domain.Entities;

public class RevisionScheduleItem
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public int TopicId { get; set; }
    public DateTime NextDueUtc { get; set; }
    
    // Navigation
    public Student Student { get; set; } = null!;
    public Topic Topic { get; set; } = null!;
}