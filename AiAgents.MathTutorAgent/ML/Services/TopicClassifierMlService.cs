using AiAgents.MathTutorAgent.ML.Models;
using Microsoft.ML;
using Microsoft.Extensions.Logging;

namespace AiAgents.MathTutorAgent.ML.Services;

public class TopicClassifierMlService(ILogger<TopicClassifierMlService> logger)
{
    private static readonly Action<ILogger, string, Exception?> LogTopicClassifierMissingMessage =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(1100, nameof(LogTopicClassifierMissingMessage)),
            "Topic classifier not found at {Path}");

    private static readonly Action<ILogger, string, Exception?> LogTopicClassifierLoadedMessage =
        LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(1101, nameof(LogTopicClassifierLoadedMessage)),
            "Topic classifier loaded from {Path}");

    private readonly MLContext _mlContext = new();
    private PredictionEngine<TopicClassificationData, TopicPrediction>? _predictionEngine;

    public async Task LoadModelAsync(string modelPath)
    {
        if (!File.Exists(modelPath))
        {
            LogTopicClassifierMissingMessage(logger, modelPath, null);
            return;
        }

        var model = _mlContext.Model.Load(modelPath, out var schema);
        _predictionEngine = _mlContext.Model.CreatePredictionEngine<TopicClassificationData, TopicPrediction>(model);

        LogTopicClassifierLoadedMessage(logger, modelPath, null);
    }

    public string PredictTopic(string questionText)
    {
        if (_predictionEngine == null)
        {
            // Fallback: keyword matching
            var lower = questionText.ToLowerInvariant();
            if (lower.Contains("solve", StringComparison.Ordinal) || lower.Contains('x'))
                return "Algebra";
            if (lower.Contains("area", StringComparison.Ordinal) ||
                lower.Contains("circle", StringComparison.Ordinal) ||
                lower.Contains("triangle", StringComparison.Ordinal))
                return "Geometry";
            if (lower.Contains("plus", StringComparison.Ordinal) ||
                lower.Contains("minus", StringComparison.Ordinal) ||
                lower.Contains("add", StringComparison.Ordinal))
                return "Arithmetic";

            return "General";
        }

        var prediction = _predictionEngine.Predict(new TopicClassificationData
        {
            QuestionText = questionText
        });

        return prediction.Topic;
    }
}
