using AiAgents.MathTutorAgent.ML.Models;
using Microsoft.ML;
using Microsoft.Extensions.Logging;

namespace AiAgents.MathTutorAgent.ML.Services;

public class TopicClassifierMlService(ILogger<TopicClassifierMlService> logger)
{
    private readonly MLContext _mlContext = new();
    private PredictionEngine<TopicClassificationData, TopicPrediction>? _predictionEngine;

    public async Task LoadModelAsync(string modelPath)
    {
        if (!File.Exists(modelPath))
        {
            logger.LogWarning("Topic classifier not found at {Path}", modelPath);
            return;
        }

        var model = _mlContext.Model.Load(modelPath, out var schema);
        _predictionEngine = _mlContext.Model.CreatePredictionEngine<TopicClassificationData, TopicPrediction>(model);

        logger.LogInformation("Topic classifier loaded from {Path}", modelPath);
    }

    public string PredictTopic(string questionText)
    {
        if (_predictionEngine == null)
        {
            // Fallback: keyword matching
            var lower = questionText.ToLower();
            if (lower.Contains("solve") || lower.Contains("x"))
                return "Algebra";
            if (lower.Contains("area") || lower.Contains("circle") || lower.Contains("triangle"))
                return "Geometry";
            if (lower.Contains("plus") || lower.Contains("minus") || lower.Contains("add"))
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