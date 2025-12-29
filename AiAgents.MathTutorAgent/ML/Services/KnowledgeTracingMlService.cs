using AiAgents.MathTutorAgent.ML.Models;
using Microsoft.ML;
using Microsoft.Extensions.Logging;

namespace AiAgents.MathTutorAgent.ML.Services;

public class KnowledgeTracingMlService(ILogger<KnowledgeTracingMlService> logger)
{
    private readonly MLContext _mlContext = new();
    private ITransformer? _model;
    private PredictionEngine<KnowledgeTracingData, KnowledgeTracingPrediction>? _predictionEngine;

    public async Task LoadModelAsync(string modelPath)
    {
        if (!File.Exists(modelPath))
        {
            logger.LogWarning("Model not found at {Path}, using default predictions", modelPath);
            return;
        }

        _model = _mlContext.Model.Load(modelPath, out var schema);
        _predictionEngine = _mlContext.Model.CreatePredictionEngine<KnowledgeTracingData, KnowledgeTracingPrediction>(_model);

        logger.LogInformation("Knowledge Tracing model loaded from {Path}", modelPath);
    }

    public float PredictMasteryChange(
        float topicDifficulty,
        float currentMastery,
        float recentAccuracy,
        float avgTimeMs,
        float daysSinceLastPractice,
        int totalAttempts,
        int consecutiveCorrect,
        int consecutiveIncorrect)
    {
        if (_predictionEngine == null)
        {
            // Fallback: simple heuristic
            var change = recentAccuracy * 10 - (1 - recentAccuracy) * 5;
            change += consecutiveCorrect * 2 - consecutiveIncorrect * 3;
            return (float)Math.Clamp(currentMastery + change, 0, 100);
        }

        var input = new KnowledgeTracingData
        {
            TopicDifficulty = topicDifficulty,
            StudentPreviousMastery = currentMastery,
            RecentAccuracy = recentAccuracy,
            AverageTimeMs = avgTimeMs,
            DaysSinceLastPractice = daysSinceLastPractice,
            TotalAttempts = totalAttempts,
            ConsecutiveCorrect = consecutiveCorrect,
            ConsecutiveIncorrect = consecutiveIncorrect
        };

        var prediction = _predictionEngine.Predict(input);
        return prediction.PredictedMastery;
    }
}