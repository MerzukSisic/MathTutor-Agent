using AiAgents.MathTutorAgent.Infrastructure;
using AiAgents.MathTutorAgent.ML.Models;
using AiAgents.MathTutorAgent.Application.Services;
using Microsoft.EntityFrameworkCore;

namespace AiAgents.MathTutorAgent.ML.Services;

public class MlTrainingDatasetBuilderService(MathTutorDbContext context)
{
    public async Task<List<KnowledgeTracingData>> BuildKnowledgeTracingDataFromAttemptsAsync(CancellationToken ct)
    {
        var attempts = await context.Attempts
            .Include(a => a.Question)
            .ThenInclude(q => q.Topic)
            .OrderBy(a => a.StudentId)
            .ThenBy(a => a.Question.TopicId)
            .ThenBy(a => a.CreatedAt)
            .ToListAsync(ct);

        var data = new List<KnowledgeTracingData>();
        var studentIds = attempts.Select(a => a.StudentId).Distinct().ToList();
        var challengeRows = await context.StudentChallengeProgress
            .Where(x => studentIds.Contains(x.StudentId))
            .OrderBy(x => x.StudentId)
            .ThenBy(x => x.CompletedAtUtc)
            .Select(x => new ChallengeSnapshot(x.StudentId, x.ChallengeKey, x.CompletedAtUtc))
            .ToListAsync(ct);

        var challengeByStudent = challengeRows
            .GroupBy(x => x.StudentId)
            .ToDictionary(g => g.Key, g => g.ToList());

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

                var studentChallenges = challengeByStudent.TryGetValue(current.StudentId, out var snapshots)
                    ? snapshots.Where(x => x.CompletedAtUtc <= current.CreatedAt).ToList()
                    : new List<ChallengeSnapshot>();
                var chapterChallengesCompleted = ChallengeChapterMapper.CountCompletedChapterChallenges(
                    studentChallenges.Select(x => x.ChallengeKey));
                var topicChapterKey = ChallengeChapterMapper.FromTopicName(current.Question.Topic?.Name ?? string.Empty);
                var topicChapterChallengeCompleted = topicChapterKey != null &&
                    studentChallenges.Any(x => string.Equals(x.ChallengeKey, topicChapterKey, StringComparison.OrdinalIgnoreCase));
                var finalChallengeCompleted = studentChallenges.Any(x =>
                    string.Equals(x.ChallengeKey, ChallengeChapterMapper.FinalMixedKey, StringComparison.OrdinalIgnoreCase));
                var latestChallengeUtc = studentChallenges.Count == 0
                    ? (DateTime?)null
                    : studentChallenges.Max(x => x.CompletedAtUtc);
                var daysSinceLastChallenge = latestChallengeUtc.HasValue
                    ? (float)Math.Max(0, (current.CreatedAt - latestChallengeUtc.Value).TotalDays)
                    : 30f;

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
                    ChapterChallengesCompleted = chapterChallengesCompleted,
                    TopicChapterChallengeCompleted = topicChapterChallengeCompleted ? 1f : 0f,
                    FinalChallengeCompleted = finalChallengeCompleted ? 1f : 0f,
                    DaysSinceLastChallenge = daysSinceLastChallenge,
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

    private sealed record ChallengeSnapshot(int StudentId, string ChallengeKey, DateTime CompletedAtUtc);
}
