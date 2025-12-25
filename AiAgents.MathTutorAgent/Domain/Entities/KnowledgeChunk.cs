namespace AiAgents.MathTutorAgent.Domain.Entities;

public class KnowledgeChunk
{
    public int Id { get; set; }
    public int DocumentId { get; set; }
    public int PageNumber { get; set; }
    public string ChunkText { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public string? EmbeddingRef { get; set; } // Reference to vector store
    
    // Navigation
    public KnowledgeDocument Document { get; set; } = null!;
}