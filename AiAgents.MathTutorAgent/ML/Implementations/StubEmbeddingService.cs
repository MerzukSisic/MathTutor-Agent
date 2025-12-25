using AiAgents.MathTutorAgent.ML.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace AiAgents.MathTutorAgent.ML.Implementations;

/// <summary>
/// Stub implementation - generates deterministic fake embeddings
/// Replace with real service (Azure OpenAI, OpenAI, Sentence Transformers)
/// </summary>
public class StubEmbeddingService : IEmbeddingService
{
    private const int EmbeddingDimension = 384; // Standard dimension for sentence transformers

    public Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken ct = default)
    {
        // Generate deterministic "fake" embedding based on text hash
        // In production: call Azure OpenAI ada-002 or use local sentence-transformers model
        
        var hash = MD5.HashData(Encoding.UTF8.GetBytes(text));
        var embedding = new float[EmbeddingDimension];
        
        for (int i = 0; i < EmbeddingDimension; i++)
        {
            embedding[i] = (hash[i % hash.Length] / 255.0f) * 2.0f - 1.0f; // Normalize to [-1, 1]
        }
        
        // Normalize vector
        var magnitude = (float)Math.Sqrt(embedding.Sum(x => x * x));
        for (int i = 0; i < EmbeddingDimension; i++)
        {
            embedding[i] /= magnitude;
        }
        
        return Task.FromResult(embedding);
    }

    public async Task<List<float[]>> GenerateEmbeddingsAsync(List<string> texts, CancellationToken ct = default)
    {
        var embeddings = new List<float[]>();
        foreach (var text in texts)
        {
            embeddings.Add(await GenerateEmbeddingAsync(text, ct));
        }
        return embeddings;
    }
}