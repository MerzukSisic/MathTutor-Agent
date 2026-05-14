using AiAgents.MathTutorAgent.Domain.Entities;
using AiAgents.MathTutorAgent.Infrastructure;
using AiAgents.MathTutorAgent.ML.Services;
using Microsoft.EntityFrameworkCore;

namespace AiAgents.MathTutorAgent.Application.Services;

public class KnowledgeTracingService
{
    private readonly MathTutorDbContext _context;
    private readonly KnowledgeTracingMlService _mlService;

    public KnowledgeTracingService(MathTutorDbContext context, KnowledgeTracingMlService mlService)
    {
        _context = context;
        _mlService = mlService;
    }

    public async Task UpdateTopicStateAsync(int studentId, Attempt attempt, CancellationToken ct = default)
    {
        var question = await _context.Questions.FindAsync(new object[] { attempt.QuestionId }, ct);
        if (question == null) return;
        var topicName = await _context.Topics
            .Where(t => t.Id == question.TopicId)
            .Select(t => t.Name)
            .FirstOrDefaultAsync(ct) ?? string.Empty;

        var state = await _context.StudentTopicStates
            .FirstOrDefaultAsync(s => s.StudentId == studentId && s.TopicId == question.TopicId, ct);

        if (state == null)
        {
            state = new StudentTopicState
            {
                StudentId = studentId,
                TopicId = question.TopicId,
                MasteryScore = 0,
                Confidence = 0.5,
                ForgettingRisk = 0,
                LastPracticedUtc = DateTime.UtcNow
            };
            _context.StudentTopicStates.Add(state);
        }

        // Calculate ML features
        float previousMastery = state.MasteryScore;
        var previousLastPracticed = state.LastPracticedUtc;
        var daysSince = (DateTime.UtcNow - previousLastPracticed).TotalDays;

        var recentAttempts = await _context.Attempts
            .Where(a => a.StudentId == studentId && a.Question.TopicId == question.TopicId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(10)
            .ToListAsync(ct);

        var recentAccuracy = recentAttempts.Any()
            ? recentAttempts.Count(a => a.IsCorrect) / (float)recentAttempts.Count
            : (attempt.IsCorrect ? 1f : 0f);

        var avgTime = recentAttempts.Any()
            ? recentAttempts.Average(a => a.TimeMs)
            : attempt.TimeMs;

        var consecutiveCorrect = 0;
        var consecutiveIncorrect = 0;
        foreach (var a in recentAttempts)
        {
            if (a.IsCorrect) consecutiveCorrect++;
            else { consecutiveIncorrect++; break; }
        }

        var challengeProgress = await _context.StudentChallengeProgress
            .Where(x => x.StudentId == studentId)
            .Select(x => new { x.ChallengeKey, x.CompletedAtUtc })
            .ToListAsync(ct);

        var chapterChallengesCompleted = ChallengeChapterMapper.CountCompletedChapterChallenges(
            challengeProgress.Select(x => x.ChallengeKey));
        var topicChapterKey = ChallengeChapterMapper.FromTopicName(topicName);
        var topicChapterChallengeCompleted = topicChapterKey != null &&
            challengeProgress.Any(x => string.Equals(x.ChallengeKey, topicChapterKey, StringComparison.OrdinalIgnoreCase));
        var finalChallengeCompleted = challengeProgress.Any(x =>
            string.Equals(x.ChallengeKey, ChallengeChapterMapper.FinalMixedKey, StringComparison.OrdinalIgnoreCase));
        var lastChallengeCompletedAt = challengeProgress.Count == 0
            ? (DateTime?)null
            : challengeProgress.Max(x => x.CompletedAtUtc);
        var daysSinceLastChallenge = lastChallengeCompletedAt.HasValue
            ? (float)Math.Max(0, (DateTime.UtcNow - lastChallengeCompletedAt.Value).TotalDays)
            : 30f;

        // ✅ USE ML MODEL TO PREDICT NEW MASTERY
        var predictedMastery = _mlService.PredictMasteryChange(
            topicDifficulty: question.Difficulty,
            currentMastery: previousMastery,
            recentAccuracy: recentAccuracy,
            avgTimeMs: (float)avgTime,
            daysSinceLastPractice: (float)daysSince,
            totalAttempts: recentAttempts.Count,
            consecutiveCorrect: consecutiveCorrect,
            consecutiveIncorrect: consecutiveIncorrect,
            chapterChallengesCompleted: chapterChallengesCompleted,
            topicChapterChallengeCompleted: topicChapterChallengeCompleted,
            finalChallengeCompleted: finalChallengeCompleted,
            daysSinceLastChallenge: daysSinceLastChallenge);

        state.MasteryScore = predictedMastery;
        state.Confidence = Math.Min(1.0, recentAttempts.Count / 10.0);
        state.ForgettingRisk = Math.Min(1.0, daysSince / 7.0);
        state.LastPracticedUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);
    }


    public async Task<double> GetMasteryScoreAsync(int studentId, int topicId, CancellationToken ct = default)
    {
        var state = await _context.StudentTopicStates
            .FirstOrDefaultAsync(s => s.StudentId == studentId && s.TopicId == topicId, ct);

        return state?.MasteryScore ?? 0;
    }

    public async Task<StudentTopicState?> GetTopicStateAsync(int studentId, int topicId, CancellationToken ct = default)
    {
        return await _context.StudentTopicStates
            .FirstOrDefaultAsync(s => s.StudentId == studentId && s.TopicId == topicId, ct);
    }
}
