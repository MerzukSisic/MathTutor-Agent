namespace AiAgents.MathTutorAgent.ML.Interfaces;

public interface IVectorSearch
{
    Task<List<VectorSearchResult>> SearchAsync(float[] queryVector, int topK = 5, CancellationToken ct = default);
    Task IndexAsync(string id, float[] vector, CancellationToken ct = default);
}

public record VectorSearchResult
{
    public string Id { get; init; } = string.Empty;
    public float Similarity { get; init; }
}