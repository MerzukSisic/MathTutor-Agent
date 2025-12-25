namespace AiAgents.MathTutorAgent.ML.Interfaces;

/// <summary>
/// Converts text into vector embeddings for semantic search
/// </summary>
public interface IEmbeddingService
{
    /// <summary>
    /// Generate embedding vector from text
    /// </summary>
    Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken ct = default);
    
    /// <summary>
    /// Batch generate embeddings
    /// </summary>
    Task<List<float[]>> GenerateEmbeddingsAsync(List<string> texts, CancellationToken ct = default);
}