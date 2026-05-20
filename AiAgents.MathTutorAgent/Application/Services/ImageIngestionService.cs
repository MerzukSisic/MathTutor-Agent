using AiAgents.MathTutorAgent.Domain.Entities;
using AiAgents.MathTutorAgent.Infrastructure;
using AiAgents.MathTutorAgent.ML.Interfaces;

namespace AiAgents.MathTutorAgent.Application.Services;

public class ImageIngestionService(
    MathTutorDbContext context,
    IImageTextExtractor ocrService,
    IEmbeddingService embeddingService,
    IVectorSearch vectorSearch)
{
    private readonly string _imageStoragePath = "wwwroot/uploads/images";

    public async Task<ImageNote> IngestImageAsync(
        int studentId,
        byte[] imageBytes,
        string fileName,
        CancellationToken ct = default)
    {
        // 1. Save image to disk
        var relativePath = await SaveImageAsync(fileName, imageBytes, ct);

        // 2. Extract text using ML-powered OCR
        var extractionResult = await ocrService.ExtractAsync(imageBytes, ct);

        // 3. Create ImageNote entity (without EmbeddingRef yet)
        var imageNote = new ImageNote
        {
            StudentId = studentId,
            UploadedAt = DateTime.UtcNow,
            ImagePath = relativePath, // ✅ FIX: Store relative path
            ExtractedText = extractionResult.ExtractedText,
            Summary = extractionResult.Summary,
            Tags = extractionResult.DetectedTopics,
            EmbeddingRef = null! // Will be set after SaveChanges
        };

        context.ImageNotes.Add(imageNote);
        await context.SaveChangesAsync(ct);

        // 4. ✅ FIX: Now set EmbeddingRef using actual ID
        imageNote.EmbeddingRef = $"image_{imageNote.Id}";

        // 5. Generate embedding for semantic search
        var textForEmbedding = $"{extractionResult.ExtractedText} {extractionResult.Summary}";
        var embedding = await embeddingService.GenerateEmbeddingAsync(textForEmbedding, ct);

        // 6. Index in vector store using consistent ID
        await vectorSearch.IndexAsync(imageNote.EmbeddingRef, embedding, ct);

        await context.SaveChangesAsync(ct);

        return imageNote;
    }

    private async Task<string> SaveImageAsync(string fileName, byte[] imageBytes, CancellationToken ct)
    {
        Directory.CreateDirectory(_imageStoragePath);
        var sanitizedFileName = Path.GetFileName(fileName);
        var uniqueFileName = $"{Guid.NewGuid()}_{sanitizedFileName}";
        var fullPath = Path.Combine(_imageStoragePath, uniqueFileName);
        await File.WriteAllBytesAsync(fullPath, imageBytes, ct);

        // ✅ FIX: Return relative path for web serving
        return $"/uploads/images/{uniqueFileName}";
    }
}
