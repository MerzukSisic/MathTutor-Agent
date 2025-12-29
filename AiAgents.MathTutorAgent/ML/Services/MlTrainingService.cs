using Microsoft.Extensions.Logging;

namespace AiAgents.MathTutorAgent.ML.Services;

/// <summary>
/// Application service for ML model training orchestration
/// </summary>
public class MlTrainingService(
    MlModelTrainer mlTrainer,
    KnowledgeTracingMlService ktMlService,
    TopicClassifierMlService topicMlService,
    ILogger<MlTrainingService> logger)
{
    /// <summary>
    /// Train all ML models with synthetic data
    /// </summary>
    public async Task<MlTrainingResult> TrainAllModelsAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Starting ML model training orchestration...");

        try
        {
            // THINK: Decide on training data size and strategy
            var ktDataCount = 2000;
            var topicDataCount = 1000;

            logger.LogInformation("Generating {Count} knowledge tracing samples", ktDataCount);
            var ktData = mlTrainer.GenerateSyntheticKnowledgeTracingData(ktDataCount);

            logger.LogInformation("Generating {Count} topic classification samples", topicDataCount);
            var topicData = mlTrainer.GenerateSyntheticTopicData(topicDataCount);

            // ACT: Train models
            var ktModelPath = await mlTrainer.TrainKnowledgeTracingModelAsync(ktData, ct);
            var topicModelPath = await mlTrainer.TrainTopicClassifierAsync(topicData, ct);

            logger.LogInformation("ML training complete, models saved");

            return new MlTrainingResult
            {
                Success = true,
                KnowledgeTracingModelPath = ktModelPath,
                TopicClassifierModelPath = topicModelPath,
                KnowledgeTracingSamplesCount = ktDataCount,
                TopicClassificationSamplesCount = topicDataCount
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ML model training failed");
            return new MlTrainingResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Reload ML models from disk
    /// </summary>
    public async Task<MlReloadResult> ReloadModelsAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Reloading ML models...");

        try
        {
            await ktMlService.LoadModelAsync("MLModels/knowledge-tracing.zip");
            await topicMlService.LoadModelAsync("MLModels/topic-classifier.zip");

            logger.LogInformation("ML models reloaded successfully");

            return new MlReloadResult
            {
                Success = true,
                Message = "ML models reloaded successfully"
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to reload ML models");
            return new MlReloadResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
}

/// <summary>
/// Result DTO for ML training
/// </summary>
public class MlTrainingResult
{
    public bool Success { get; set; }
    public string? KnowledgeTracingModelPath { get; set; }
    public string? TopicClassifierModelPath { get; set; }
    public int KnowledgeTracingSamplesCount { get; set; }
    public int TopicClassificationSamplesCount { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Result DTO for ML reload
/// </summary>
public class MlReloadResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
}