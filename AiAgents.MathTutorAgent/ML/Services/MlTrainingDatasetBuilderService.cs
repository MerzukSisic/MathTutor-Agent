using AiAgents.MathTutorAgent.Infrastructure;
using AiAgents.MathTutorAgent.ML.Models;
using Microsoft.EntityFrameworkCore;

namespace AiAgents.MathTutorAgent.ML.Services;

public class MlTrainingDatasetBuilderService(MathTutorDbContext context)
{
    public async Task<List<KnowledgeTracingData>> BuildKnowledgeTracingDataFromAttemptsAsync(CancellationToken ct)
    {
        var attempts = await context.Attempts
            .Include(a => a.Question)
            .OrderBy(a => a.StudentId)
            .ThenBy(a => a.Question.TopicId)
            .ThenBy(a => a.CreatedAt)
            .ToListAsync(ct);

        var data = new List<KnowledgeTracingData>();

        foreach (var group in attempts.GroupBy(a => new { a.StudentId, a.Question.TopicId }))
        {
            var topicAttempts = group.ToList();
            if (topicAttempts.Count < 2)
            {
                continue;
            }

            for (var i = 0; i < topicAttempts.Count; i++)
            {
                var current = topicAttempts[i];
                var history = topicAttempts.Take(i + 1).ToList();
                var window = history.TakeLast(10).ToList();

                var recentAccuracy = window.Count(a => a.IsCorrect) / (float)window.Count;
                var avgTime = (float)window.Average(a => a.TimeMs);
                var consecutiveCorrect = CountTrailing(window, true);
                var consecutiveIncorrect = CountTrailing(window, false);

                var previousMastery = i == 0
                    ? 0f
                    : topicAttempts.Take(i).Count(a => a.IsCorrect) / (float)i * 100f;

                var predictedMastery = history.Count(a => a.IsCorrect) / (float)history.Count * 100f;

                var daysSinceLastPractice = i == 0
                    ? 0f
                    : (float)(current.CreatedAt - topicAttempts[i - 1].CreatedAt).TotalDays;

                data.Add(new KnowledgeTracingData
                {
                    TopicDifficulty = current.Question.Difficulty,
                    StudentPreviousMastery = previousMastery,
                    RecentAccuracy = recentAccuracy,
                    AverageTimeMs = avgTime,
                    DaysSinceLastPractice = Math.Max(0, daysSinceLastPractice),
                    TotalAttempts = history.Count,
                    ConsecutiveCorrect = consecutiveCorrect,
                    ConsecutiveIncorrect = consecutiveIncorrect,
                    PredictedMastery = predictedMastery
                });
            }
        }

        return data;
    }

    public async Task<List<TopicClassificationData>> BuildTopicClassificationDataFromQuestionsAsync(CancellationToken ct)
    {
        return await context.Questions
            .Include(q => q.Topic)
            .Select(q => new TopicClassificationData
            {
                QuestionText = q.QuestionText,
                TopicLabel = q.Topic.Area.ToString()
            })
            .ToListAsync(ct);
    }

    private static int CountTrailing(List<AiAgents.MathTutorAgent.Domain.Entities.Attempt> attempts, bool isCorrect)
    {
        var count = 0;
        for (var i = attempts.Count - 1; i >= 0; i--)
        {
            if (attempts[i].IsCorrect != isCorrect)
            {
                break;
            }

            count++;
        }

        return count;
    }
}
