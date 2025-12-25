using AiAgents.MathTutorAgent.Application.DTOs;
using AiAgents.MathTutorAgent.Infrastructure;
using AiAgents.MathTutorAgent.ML.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AiAgents.MathTutorAgent.Application.Services;

public class ExplanationService(
    MathTutorDbContext context,
    IEmbeddingService embeddingService,
    IVectorSearch vectorSearch)
{
    public async Task<ExplanationDto> RetrieveAndComposeExplanationAsync(
        int studentId, 
        int? questionId, 
        int? topicId, 
        string? errorTag,
        CancellationToken ct = default)
    {
        var references = new List<ReferenceDto>();
        
        // 1. Build semantic query
        string queryText = BuildQueryText(questionId, topicId, errorTag);
        
        // 2. Generate query embedding for semantic search
        var queryEmbedding = await embeddingService.GenerateEmbeddingAsync(queryText, ct);
        
        // 3. Search for semantically similar knowledge chunks using vector search
        var vectorResults = await vectorSearch.SearchAsync(queryEmbedding, topK: 5, ct);
        
        // 4. Retrieve matching knowledge chunks from database
        var chunkIds = vectorResults
            .Where(r => r.Id.StartsWith("chunk_"))
            .Select(r => int.Parse(r.Id.Replace("chunk_", "")))
            .ToList();
            
        if (chunkIds.Any())
        {
            var chunks = await context.KnowledgeChunks
                .Include(c => c.Document)
                .Where(c => chunkIds.Contains(c.Id))
                .Take(3)
                .ToListAsync(ct);

            foreach (var chunk in chunks)
            {
                references.Add(new ReferenceDto
                {
                    Type = "Document",
                    Id = chunk.DocumentId,
                    Title = chunk.Document.Title,
                    PageOrTime = $"Page {chunk.PageNumber}"
                });
            }
        }

        // 5. Find relevant student image notes using vector search
        var imageNoteIds = vectorResults
            .Where(r => r.Id.StartsWith("image_"))
            .Select(r => int.Parse(r.Id.Replace("image_", "")))
            .ToList();
            
        if (imageNoteIds.Any())
        {
            var imageNotes = await context.ImageNotes
                .Where(i => i.StudentId == studentId && imageNoteIds.Contains(i.Id))
                .OrderByDescending(i => i.UploadedAt)
                .Take(2)
                .ToListAsync(ct);

            foreach (var note in imageNotes)
            {
                references.Add(new ReferenceDto
                {
                    Type = "ImageNote",
                    Id = note.Id,
                    Title = $"Your Screenshot #{note.Id}",
                    PageOrTime = note.UploadedAt.ToString("yyyy-MM-dd HH:mm")
                });
            }
        }

        // 6. ✅ FIX: Fallback using topic NAME instead of ID
        if (!references.Any() && topicId.HasValue)
        {
            var topic = await context.Topics.FindAsync(new object[] { topicId.Value }, ct);
            if (topic != null)
            {
                var topicName = topic.Name;
                var topicArea = topic.Area.ToString();

                var fallbackChunks = await context.KnowledgeChunks
                    .Include(c => c.Document)
                    .Where(c => c.Tags.Any(t => t == topicName || t == topicArea))
                    .Take(2)
                    .ToListAsync(ct);

                foreach (var chunk in fallbackChunks)
                {
                    references.Add(new ReferenceDto
                    {
                        Type = "Document",
                        Id = chunk.DocumentId,
                        Title = chunk.Document.Title,
                        PageOrTime = $"Page {chunk.PageNumber}"
                    });
                }
            }
        }

        // 7. Compose explanation
        string explanation = ComposeExplanation(topicId, errorTag, references.Any());

        return new ExplanationDto
        {
            Explanation = explanation,
            Example = GenerateExample(topicId),
            Sources = references
        };
    }

    private string BuildQueryText(int? questionId, int? topicId, string? errorTag)
    {
        var parts = new List<string>();
        
        if (topicId.HasValue)
            parts.Add($"topic {topicId.Value}");
        
        if (!string.IsNullOrEmpty(errorTag))
            parts.Add($"error {errorTag}");
        
        if (questionId.HasValue)
            parts.Add($"question {questionId.Value}");
            
        return parts.Any() ? string.Join(" ", parts) : "general math help";
    }

    private string ComposeExplanation(int? topicId, string? errorTag, bool hasReferences)
    {
        if (hasReferences)
        {
            return "Based on the relevant materials, here's an explanation of this concept. " +
                   "Review the referenced sources for detailed examples and step-by-step guidance.";
        }
        
        if (topicId.HasValue)
        {
            return $"This concept is related to topic {topicId}. " +
                   "Review the foundational principles and practice similar problems. " +
                   "Consider uploading screenshots of relevant examples for personalized guidance.";
        }
        
        return "Let's break down this problem step by step. " +
               "Focus on understanding each component before moving to the next.";
    }

    private string GenerateExample(int? topicId)
    {
        return topicId switch
        {
            1 => "Example: 5 + 3 = 8. Break it down: start with 5, then count 3 more.",
            2 => "Example: 10 - 4 = 6. Start with 10, count back 4 steps.",
            3 => "Example: 6 × 7 = 42. Think of it as 6 groups of 7.",
            _ => "Example: Apply the same method you used in similar problems."
        };
    }
}