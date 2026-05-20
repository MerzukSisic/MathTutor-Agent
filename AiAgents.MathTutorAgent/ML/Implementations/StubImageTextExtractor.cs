using AiAgents.MathTutorAgent.ML.Interfaces;

namespace AiAgents.MathTutorAgent.ML.Implementations;

/// <summary>
/// Stub OCR implementation
/// Replace with Azure Computer Vision, Tesseract OCR, or Mathpix
/// </summary>
public class StubImageTextExtractor : IImageTextExtractor
{
    public Task<ImageExtractionResult> ExtractAsync(byte[] imageBytes, CancellationToken ct = default)
    {
        // Stub: return placeholder data
        // In production: use Azure Computer Vision API, Tesseract, or Mathpix API

        var result = new ImageExtractionResult
        {
            ExtractedText = "[OCR Placeholder] Mathematical equation or text from image",
            Summary = "This image appears to contain mathematical notation",
            MathTokens = new List<string> { "equation", "variable", "operator" },
            DetectedTopics = new List<string> { "Algebra", "Equations" },
            Confidence = 0.85
        };

        return Task.FromResult(result);
    }
}