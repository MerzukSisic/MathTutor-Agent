namespace AiAgents.MathTutorAgent.ML.Interfaces;

/// <summary>
/// Extracts text and mathematical expressions from images
/// </summary>
public interface IImageTextExtractor
{
    /// <summary>
    /// Extract text from image using OCR
    /// </summary>
    Task<ImageExtractionResult> ExtractAsync(byte[] imageBytes, CancellationToken ct = default);
}

public class ImageExtractionResult
{
    public string ExtractedText { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public List<string> MathTokens { get; set; } = new();
    public List<string> DetectedTopics { get; set; } = new();
    public double Confidence { get; set; }
}