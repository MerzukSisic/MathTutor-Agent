using AiAgents.MathTutorAgent.ML.Models;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.Extensions.Logging;

namespace AiAgents.MathTutorAgent.ML.Services;

public class MlModelTrainer
{
    private readonly MLContext _mlContext;
    private readonly ILogger<MlModelTrainer> _logger;
    private readonly string _modelDirectory = "MLModels";

    public MlModelTrainer(ILogger<MlModelTrainer> logger)
    {
        _mlContext = new MLContext(seed: 0);
        _logger = logger;
        Directory.CreateDirectory(_modelDirectory);
    }

    /// <summary>
    /// Train Knowledge Tracing Model (Regression)
    /// </summary>
    public async Task<string> TrainKnowledgeTracingModelAsync(
        IEnumerable<KnowledgeTracingData> trainingData,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Starting Knowledge Tracing model training...");

        var dataView = _mlContext.Data.LoadFromEnumerable(trainingData);

        // Data preprocessing pipeline
        var pipeline = _mlContext.Transforms.Concatenate(
                "Features",
                nameof(KnowledgeTracingData.TopicDifficulty),
                nameof(KnowledgeTracingData.StudentPreviousMastery),
                nameof(KnowledgeTracingData.RecentAccuracy),
                nameof(KnowledgeTracingData.AverageTimeMs),
                nameof(KnowledgeTracingData.DaysSinceLastPractice),
                nameof(KnowledgeTracingData.TotalAttempts),
                nameof(KnowledgeTracingData.ConsecutiveCorrect),
                nameof(KnowledgeTracingData.ConsecutiveIncorrect),
                nameof(KnowledgeTracingData.ChapterChallengesCompleted),
                nameof(KnowledgeTracingData.TopicChapterChallengeCompleted),
                nameof(KnowledgeTracingData.FinalChallengeCompleted),
                nameof(KnowledgeTracingData.DaysSinceLastChallenge))
            .Append(_mlContext.Transforms.NormalizeMinMax("Features"))
            .Append(_mlContext.Regression.Trainers.FastTree(
                labelColumnName: nameof(KnowledgeTracingData.PredictedMastery),
                numberOfLeaves: 20,
                minimumExampleCountPerLeaf: 10,
                learningRate: 0.2));

        // Train model
        var model = pipeline.Fit(dataView);

        // Evaluate
        var predictions = model.Transform(dataView);
        var metrics = _mlContext.Regression.Evaluate(
            predictions,
            labelColumnName: nameof(KnowledgeTracingData.PredictedMastery));

        _logger.LogInformation("Model trained - R²: {RSquared:F3}, MAE: {MAE:F2}",
            metrics.RSquared, metrics.MeanAbsoluteError);

        // Save model
        var modelPath = Path.Combine(_modelDirectory, "knowledge-tracing.zip");
        _mlContext.Model.Save(model, dataView.Schema, modelPath);

        _logger.LogInformation("Model saved to {Path}", modelPath);
        return modelPath;
    }

    /// <summary>
    /// Train Topic Classification Model (Multi-class)
    /// </summary>
    public async Task<string> TrainTopicClassifierAsync(
        IEnumerable<TopicClassificationData> trainingData,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Starting Topic Classifier training...");

        var dataView = _mlContext.Data.LoadFromEnumerable(trainingData);

        var pipeline = _mlContext.Transforms.Conversion.MapValueToKey(
                "Label",
                nameof(TopicClassificationData.TopicLabel))
            .Append(_mlContext.Transforms.Text.FeaturizeText(
                "Features",
                nameof(TopicClassificationData.QuestionText)))
            .Append(_mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy())
            .Append(_mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

        var model = pipeline.Fit(dataView);

        // Evaluate
        var predictions = model.Transform(dataView);
        var metrics = _mlContext.MulticlassClassification.Evaluate(predictions);

        _logger.LogInformation("Classifier trained - Accuracy: {Accuracy:F3}",
            metrics.MacroAccuracy);

        // Save
        var modelPath = Path.Combine(_modelDirectory, "topic-classifier.zip");
        _mlContext.Model.Save(model, dataView.Schema, modelPath);

        return modelPath;
    }

    /// <summary>
    /// Generate synthetic training data for Knowledge Tracing
    /// </summary>
    public List<KnowledgeTracingData> GenerateSyntheticKnowledgeTracingData(int count = 1000)
    {
        var random = new Random(42);
        var data = new List<KnowledgeTracingData>();

        for (int i = 0; i < count; i++)
        {
            var previousMastery = (float)(random.NextDouble() * 100);
            var recentAccuracy = (float)(random.NextDouble());
            var consecutiveCorrect = random.Next(0, 10);
            var consecutiveIncorrect = random.Next(0, 5);
            var daysSince = random.Next(0, 30);
            var chapterChallengesCompleted = random.Next(0, 5);
            var topicChapterCompleted = random.NextDouble() < 0.45 ? 1f : 0f;
            var finalChallengeCompleted = chapterChallengesCompleted >= 4 && random.NextDouble() < 0.35 ? 1f : 0f;
            var daysSinceLastChallenge = chapterChallengesCompleted == 0 ? 30f : random.Next(0, 21);

            // Simulate mastery change
            var masteryChange = recentAccuracy * 10 - (1 - recentAccuracy) * 5;
            masteryChange += consecutiveCorrect * 2 - consecutiveIncorrect * 3;
            masteryChange -= daysSince * 0.5f; // Forgetting
            masteryChange += chapterChallengesCompleted * 0.8f;
            masteryChange += topicChapterCompleted * 1.4f;
            masteryChange += finalChallengeCompleted * 2.2f;
            masteryChange -= Math.Min(4f, daysSinceLastChallenge * 0.15f);

            var newMastery = Math.Clamp(previousMastery + masteryChange, 0, 100);

            data.Add(new KnowledgeTracingData
            {
                TopicDifficulty = random.Next(1, 6),
                StudentPreviousMastery = previousMastery,
                RecentAccuracy = recentAccuracy,
                AverageTimeMs = (float)(random.NextDouble() * 60000 + 5000),
                DaysSinceLastPractice = daysSince,
                TotalAttempts = random.Next(1, 100),
                ConsecutiveCorrect = consecutiveCorrect,
                ConsecutiveIncorrect = consecutiveIncorrect,
                ChapterChallengesCompleted = chapterChallengesCompleted,
                TopicChapterChallengeCompleted = topicChapterCompleted,
                FinalChallengeCompleted = finalChallengeCompleted,
                DaysSinceLastChallenge = daysSinceLastChallenge,
                PredictedMastery = newMastery
            });
        }

        return data;
    }

    /// <summary>
    /// Generate synthetic topic classification data
    /// </summary>
    public List<TopicClassificationData> GenerateSyntheticTopicData(int count = 500)
    {
        var data = new List<TopicClassificationData>
        {
            // Arithmetic examples
            new() { QuestionText = "What is 5 plus 3?", TopicLabel = "Arithmetic" },
            new() { QuestionText = "Calculate 12 - 7", TopicLabel = "Arithmetic" },
            new() { QuestionText = "Multiply 6 by 4", TopicLabel = "Arithmetic" },
            new() { QuestionText = "Divide 20 by 5", TopicLabel = "Arithmetic" },

            // Algebra examples
            new() { QuestionText = "Solve for x: 2x + 5 = 13", TopicLabel = "Algebra" },
            new() { QuestionText = "Simplify: 3(x + 2) - 4x", TopicLabel = "Algebra" },
            new() { QuestionText = "Factor: x² - 5x + 6", TopicLabel = "Algebra" },

            // Geometry examples
            new() { QuestionText = "Find the area of a circle with radius 5", TopicLabel = "Geometry" },
            new() { QuestionText = "Calculate the perimeter of a rectangle 4x6", TopicLabel = "Geometry" },

            // Logic examples
            new() { QuestionText = "Is this a tautology: p OR NOT p", TopicLabel = "Logic" },
            new() { QuestionText = "Truth table for AND operation", TopicLabel = "Logic" }
        };

        // Repeat and vary to reach count
        var result = new List<TopicClassificationData>();
        while (result.Count < count)
        {
            result.AddRange(data);
        }

        return result.Take(count).ToList();
    }
}
