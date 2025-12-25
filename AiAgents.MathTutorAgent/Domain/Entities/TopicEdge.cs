namespace AiAgents.MathTutorAgent.Domain.Entities;

public class TopicEdge
{
    public int Id { get; set; }
    public int PrerequisiteTopicId { get; set; }
    public int DependentTopicId { get; set; }
    
    // Navigation
    public Topic PrerequisiteTopic { get; set; } = null!;
    public Topic DependentTopic { get; set; } = null!;
}