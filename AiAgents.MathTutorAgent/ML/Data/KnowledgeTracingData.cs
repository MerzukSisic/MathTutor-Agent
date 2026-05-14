using Microsoft.ML.Data;

namespace AiAgents.MathTutorAgent.ML.Models;

/// <summary>
/// Input data for knowledge tracing model
/// </summary>
public class KnowledgeTracingData
{
    [LoadColumn(0)]
    public float TopicDifficulty { get; set; }

    [LoadColumn(1)]
    public float StudentPreviousMastery { get; set; }

    [LoadColumn(2)]
    public float RecentAccuracy { get; set; }

    [LoadColumn(3)]
    public float AverageTimeMs { get; set; }

    [LoadColumn(4)]
    public float DaysSinceLastPractice { get; set; }

    [LoadColumn(5)]
    public float TotalAttempts { get; set; }

    [LoadColumn(6)]
    public float ConsecutiveCorrect { get; set; }

    [LoadColumn(7)]
    public float ConsecutiveIncorrect { get; set; }

    [LoadColumn(8)]
    public float ChapterChallengesCompleted { get; set; }

    [LoadColumn(9)]
    public float TopicChapterChallengeCompleted { get; set; }

    [LoadColumn(10)]
    public float FinalChallengeCompleted { get; set; }

    [LoadColumn(11)]
    public float DaysSinceLastChallenge { get; set; }

    // Label: predicted mastery after this attempt
    [LoadColumn(12)]
    public float PredictedMastery { get; set; }
}

/// <summary>
/// Prediction output
/// </summary>
public class KnowledgeTracingPrediction
{
    [ColumnName("Score")]
    public float PredictedMastery { get; set; }
}

/// <summary>
/// Topic classification data
/// </summary>
public class TopicClassificationData
{
    [LoadColumn(0)]
    public string QuestionText { get; set; } = string.Empty;

    [LoadColumn(1)]
    public string TopicLabel { get; set; } = string.Empty;
}

public class TopicPrediction
{
    [ColumnName("PredictedLabel")]
    public string Topic { get; set; } = string.Empty;

    public float[] Score { get; set; } = Array.Empty<float>();
}
