using AiAgents.MathTutorAgent.Domain.Enums;

namespace AiAgents.MathTutorAgent.Domain.Entities;

public class Topic
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public MathArea Area { get; set; }
    public int DifficultyBand { get; set; } // 1-5
    public string Description { get; set; } = string.Empty;
    
    // Navigation
    public List<TopicEdge> Prerequisites { get; set; } = new(); // Incoming edges
    public List<TopicEdge> Dependents { get; set; } = new();    // Outgoing edges
    public List<Question> Questions { get; set; } = new();
}