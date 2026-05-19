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
    private static readonly Action<ILogger, Exception?> LogTrainingStartedMessage =
        LoggerMessage.Define(
            LogLevel.Information,
            new EventId(1000, nameof(LogTrainingStartedMessage)),
            "Starting ML model training orchestration...");

    private static readonly Action<ILogger, int, int, Exception?> LogKtDataPreparedMessage =
        LoggerMessage.Define<int, int>(
            LogLevel.Information,
            new EventId(1001, nameof(LogKtDataPreparedMessage)),
            "Knowledge tracing training data prepared. Real: {Real}, Total: {Total}");

    private static readonly Action<ILogger, int, int, Exception?> LogTopicDataPreparedMessage =
        LoggerMessage.Define<int, int>(
            LogLevel.Information,
            new EventId(1002, nameof(LogTopicDataPreparedMessage)),
            "Topic classification training data prepared. Real: {Real}, Total: {Total}");

    private static readonly Action<ILogger, Exception?> LogTrainingCompletedMessage =
        LoggerMessage.Define(
            LogLevel.Information,
            new EventId(1003, nameof(LogTrainingCompletedMessage)),
            "ML training complete, models saved");

    private static readonly Action<ILogger, Exception?> LogReloadStartedMessage =
        LoggerMessage.Define(
            LogLevel.Information,
            new EventId(1004, nameof(LogReloadStartedMessage)),
            "Reloading ML models...");

    private static readonly Action<ILogger, Exception?> LogReloadCompletedMessage =
        LoggerMessage.Define(
            LogLevel.Information,
            new EventId(1005, nameof(LogReloadCompletedMessage)),
            "ML models reloaded successfully");

    /// <summary>
    /// Train all ML models using real attempts + synthetic fallback
    /// </summary>
    public async Task<MlTrainingResult> TrainAllModelsAsync(CancellationToken ct = default)
    {
        LogTrainingStartedMessage(logger, null);

        try
        {
            var realKtData = await datasetBuilder.BuildKnowledgeTracingDataFromAttemptsAsync(ct);
            var syntheticKtData = mlTrainer.GenerateSyntheticKnowledgeTracingData(2000);

            var finalKtData = realKtData.Count >= 400
                ? realKtData
                : realKtData.Concat(syntheticKtData.Take(2000 - realKtData.Count)).ToList();

            LogKtDataPreparedMessage(logger, realKtData.Count, finalKtData.Count, null);

            var realTopicData = await datasetBuilder.BuildTopicClassificationDataFromQuestionsAsync(ct);
            var syntheticTopicData = mlTrainer.GenerateSyntheticTopicData(1000);

            var finalTopicData = realTopicData.Count >= 300
                ? realTopicData
                : realTopicData.Concat(syntheticTopicData.Take(1000 - realTopicData.Count)).ToList();

            LogTopicDataPreparedMessage(logger, realTopicData.Count, finalTopicData.Count, null);

            var ktModelPath = await mlTrainer.TrainKnowledgeTracingModelAsync(finalKtData, ct);
            var topicModelPath = await mlTrainer.TrainTopicClassifierAsync(finalTopicData, ct);

            LogTrainingCompletedMessage(logger, null);

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
        LogReloadStartedMessage(logger, null);

        try
        {
            await ktMlService.LoadModelAsync("MLModels/knowledge-tracing.zip");
            await topicMlService.LoadModelAsync("MLModels/topic-classifier.zip");

            LogReloadCompletedMessage(logger, null);

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
