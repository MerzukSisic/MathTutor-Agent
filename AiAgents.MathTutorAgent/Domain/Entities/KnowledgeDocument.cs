namespace AiAgents.MathTutorAgent.Domain.Entities;

public class KnowledgeDocument
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation
    public List<KnowledgeChunk> Chunks { get; set; } = new();
}