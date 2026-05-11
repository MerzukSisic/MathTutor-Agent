using Microsoft.Extensions.Logging;

namespace AiAgents.MathTutorAgent.ML.Services;

/// <summary>
/// Application service for ML model training orchestration
/// </summary>
public class MlTrainingService(
    MlTrainingDatasetBuilderService datasetBuilder,
    MlModelTrainer mlTrainer,
    KnowledgeTracingMlService ktMlService,
    TopicClassifierMlService topicMlService,
    ILogger<MlTrainingService> logger)
{
    /// <summary>
    /// Train all ML models using real attempts + synthetic fallback
    /// </summary>
    public async Task<MlTrainingResult> TrainAllModelsAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Starting ML model training orchestration...");

        try
        {
            var realKtData = await datasetBuilder.BuildKnowledgeTracingDataFromAttemptsAsync(ct);
            var syntheticKtData = mlTrainer.GenerateSyntheticKnowledgeTracingData(2000);

            var finalKtData = realKtData.Count >= 400
                ? realKtData
                : realKtData.Concat(syntheticKtData.Take(2000 - realKtData.Count)).ToList();

            logger.LogInformation(
                "Knowledge tracing training data prepared. Real: {Real}, Total: {Total}",
                realKtData.Count,
                finalKtData.Count);

            var realTopicData = await datasetBuilder.BuildTopicClassificationDataFromQuestionsAsync(ct);
            var syntheticTopicData = mlTrainer.GenerateSyntheticTopicData(1000);

            var finalTopicData = realTopicData.Count >= 300
                ? realTopicData
                : realTopicData.Concat(syntheticTopicData.Take(1000 - realTopicData.Count)).ToList();

            logger.LogInformation(
                "Topic classification training data prepared. Real: {Real}, Total: {Total}",
                realTopicData.Count,
                finalTopicData.Count);

            var ktModelPath = await mlTrainer.TrainKnowledgeTracingModelAsync(finalKtData, ct);
            var topicModelPath = await mlTrainer.TrainTopicClassifierAsync(finalTopicData, ct);

            logger.LogInformation("ML training complete, models saved");

            return new MlTrainingResult
            {
                Success = true,
                KnowledgeTracingModelPath = ktModelPath,
                TopicClassifierModelPath = topicModelPath,
                KnowledgeTracingSamplesCount = finalKtData.Count,
                TopicClassificationSamplesCount = finalTopicData.Count,
                RealKnowledgeTracingSamplesCount = realKtData.Count,
                RealTopicClassificationSamplesCount = realTopicData.Count
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
