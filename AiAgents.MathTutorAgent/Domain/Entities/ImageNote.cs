namespace AiAgents.MathTutorAgent.Domain.Entities;

public class ImageNote
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public string ImagePath { get; set; } = string.Empty;
    public string ExtractedText { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public string? EmbeddingRef { get; set; }

    // Navigation
    public Student Student { get; set; } = null!;
}