using AiAgents.MathTutorAgent.Domain.Enums;

namespace AiAgents.MathTutorAgent.Domain.Entities;

public class WorkItem
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public WorkItemType Type { get; set; }
    public WorkStatus Status { get; set; } = WorkStatus.Queued;
    public string PayloadJson { get; set; } = string.Empty; // Store request data
    public string? ResultJson { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }

    // Navigation
    public Student Student { get; set; } = null!;
}