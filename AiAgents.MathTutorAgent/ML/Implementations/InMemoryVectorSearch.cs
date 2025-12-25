using AiAgents.MathTutorAgent.ML.Interfaces;
using System.Collections.Concurrent;

namespace AiAgents.MathTutorAgent.ML.Implementations;

public class InMemoryVectorSearch : IVectorSearch
{
    private readonly ConcurrentDictionary<string, float[]> _vectors = new();

    public Task<List<VectorSearchResult>> SearchAsync(float[] queryVector, int topK = 5, CancellationToken ct = default)
    {
        var results = _vectors
            .Select(kvp => new
            {
                Id = kvp.Key,
                Similarity = CosineSimilarity(queryVector, kvp.Value)
            })
            .OrderByDescending(x => x.Similarity)
            .Take(topK)
            .Select(x => new VectorSearchResult
            {
                Id = x.Id,
                Similarity = x.Similarity
            })
            .ToList();

        return Task.FromResult(results);
    }

    public Task IndexAsync(string id, float[] vector, CancellationToken ct = default)
    {
        _vectors[id] = vector;
        return Task.CompletedTask;
    }

    private static float CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length)
            return 0;

        float dotProduct = 0;
        float magnitudeA = 0;
        float magnitudeB = 0;

        for (int i = 0; i < a.Length; i++)
        {
            dotProduct += a[i] * b[i];
            magnitudeA += a[i] * a[i];
            magnitudeB += b[i] * b[i];
        }

        if (magnitudeA == 0 || magnitudeB == 0)
            return 0;

        return dotProduct / (float)(Math.Sqrt(magnitudeA) * Math.Sqrt(magnitudeB));
    }
}