using AiAgents.MathTutorAgent.Domain.Entities;
using AiAgents.MathTutorAgent.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AiAgents.MathTutorAgent.Application.Services;

public class QuestionSelectionService(
    MathTutorDbContext context,
    QuestionGenerationService questionGenerationService,
    QuestionDifficultyAdvisorService difficultyAdvisor)
{
    public async Task<Question?> SelectNextQuestionAsync(
        int studentId,
        int topicId,
        double masteryScore,
        CancellationToken ct = default)
    {
        var initialDifficulty = difficultyAdvisor.GetTargetDifficultyFromMastery(masteryScore);

        var allAttempts = await context.Attempts
            .Where(a => a.StudentId == studentId && a.Question.TopicId == topicId)
            .OrderBy(a => a.CreatedAt)
            .Select(a => new AttemptSelectionSnapshot(
                a.QuestionId,
                a.IsCorrect,
                a.CreatedAt,
                a.TimeMs))
            .ToListAsync(ct);

        var paceSnapshots = allAttempts
            .Select(a => new AttemptPaceSnapshot(a.IsCorrect, a.TimeMs))
            .ToList();
        var topicName = await context.Topics
            .Where(t => t.Id == topicId)
            .Select(t => t.Name)
            .FirstOrDefaultAsync(ct) ?? string.Empty;
        var chapterKey = ChallengeChapterMapper.FromTopicName(topicName);
        var completedChallengeKeys = await context.StudentChallengeProgress
            .Where(x => x.StudentId == studentId)
            .Select(x => x.ChallengeKey)
            .ToListAsync(ct);
        var challengeSignals = new ChallengePaceSignals(
            ChapterChallengesCompleted: ChallengeChapterMapper.CountCompletedChapterChallenges(completedChallengeKeys),
            TopicChapterChallengeCompleted: chapterKey != null &&
                                           completedChallengeKeys.Contains(chapterKey, StringComparer.OrdinalIgnoreCase),
            FinalChallengeCompleted: completedChallengeKeys.Contains(ChallengeChapterMapper.FinalMixedKey, StringComparer.OrdinalIgnoreCase));

        var targetDifficulty = difficultyAdvisor.AdaptDifficultyByPace(initialDifficulty, paceSnapshots, challengeSignals);

        var totalAttempts = allAttempts.Count;
        var attemptedQuestionIds = allAttempts
            .Select(a => a.QuestionId)
            .ToHashSet();

        var lastAttemptPerQuestion = allAttempts
            .GroupBy(a => a.QuestionId)
            .ToDictionary(
                g => g.Key,
                g => new
                {
                    LastAttempt = g.OrderByDescending(x => x.CreatedAt).First(),
                    Index = allAttempts.FindLastIndex(x => x.QuestionId == g.Key)
                });

        bool IsReady(int questionId)
        {
            if (!lastAttemptPerQuestion.ContainsKey(questionId))
            {
                return true;
            }

            var info = lastAttemptPerQuestion[questionId];
            var attemptsSince = totalAttempts - info.Index - 1;
            var requiredGap = info.LastAttempt.IsCorrect ? 28 : 8;
            return attemptsSince >= requiredGap;
        }

        var allQuestions = await context.Questions
            .Where(q => q.TopicId == topicId)
            .ToListAsync(ct);

        if (allQuestions.Count == 0)
        {
            return null;
        }

        var readyQuestions = allQuestions
            .Where(q => IsReady(q.Id))
            .ToList();

        var unseenReadyQuestions = readyQuestions
            .Where(q => !attemptedQuestionIds.Contains(q.Id))
            .ToList();

        if (unseenReadyQuestions.Count > 0)
        {
            var bestUnseen = unseenReadyQuestions
                .OrderBy(q => Math.Abs(q.Difficulty - targetDifficulty))
                .ThenBy(_ => Guid.NewGuid())
                .First();
            return bestUnseen;
        }

        var attemptedQuestionCount = allAttempts.Select(a => a.QuestionId).Distinct().Count();
        var topicCoverage = (double)attemptedQuestionCount / allQuestions.Count;
        var poolLooksSmall = allQuestions.Count < 24;

        // Generate fresh items earlier when student already covered good chunk or pool is narrow.
        if ((topicCoverage >= 0.45 && readyQuestions.Count <= 5) || (topicCoverage >= 0.25 && poolLooksSmall))
        {
            var generated = await questionGenerationService.GenerateQuestionAsync(topicId, targetDifficulty, ct);
            if (generated != null)
            {
                return generated;
            }
        }

        if (readyQuestions.Count > 0)
        {
            // If we must repeat, pick least repeated and least recent question first.
            var attemptCountByQuestion = allAttempts
                .GroupBy(a => a.QuestionId)
                .ToDictionary(g => g.Key, g => g.Count());

            return readyQuestions
                .OrderBy(q => attemptCountByQuestion.TryGetValue(q.Id, out var count) ? count : 0)
                .ThenBy(q => lastAttemptPerQuestion.TryGetValue(q.Id, out var info)
                    ? info.LastAttempt.CreatedAt
                    : DateTime.MinValue)
                .ThenBy(q => Math.Abs(q.Difficulty - targetDifficulty))
                .ThenBy(_ => Guid.NewGuid())
                .First();
        }

        return allQuestions
            .OrderBy(q => lastAttemptPerQuestion.ContainsKey(q.Id)
                ? lastAttemptPerQuestion[q.Id].Index
                : -1)
            .ThenBy(_ => Guid.NewGuid())
            .First();
    }

    private sealed record AttemptSelectionSnapshot(
        int QuestionId,
        bool IsCorrect,
        DateTime CreatedAt,
        int TimeMs);
}
