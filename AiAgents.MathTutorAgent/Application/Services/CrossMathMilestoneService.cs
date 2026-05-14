using AiAgents.MathTutorAgent.Application.DTOs;
using AiAgents.MathTutorAgent.Domain.Entities;
using AiAgents.MathTutorAgent.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AiAgents.MathTutorAgent.Application.Services;

public class CrossMathMilestoneService(MathTutorDbContext context)
{
    private const double TopicCompletionMasteryThreshold = 75.0;
    private const int TopicCompletionMinUniqueQuestions = 6;

    public async Task<CrossMathMilestoneDto?> GetChallengeToStartAfterAnswerAsync(
        int studentId,
        int topicId,
        double updatedMasteryScore,
        CancellationToken ct = default)
    {
        if (await ShouldOfferFinalMilestoneAsync(studentId, ct))
        {
            return BuildFinalMilestone();
        }

        var chapterKey = await ResolveChapterKeyFromTopicAsync(topicId, ct);
        if (chapterKey == null)
        {
            return null;
        }

        var alreadyCompleted = await context.StudentChallengeProgress
            .AsNoTracking()
            .AnyAsync(x => x.StudentId == studentId && x.ChallengeKey == chapterKey, ct);
        if (alreadyCompleted)
        {
            return null;
        }

        var chapterCompleted = await IsTopicCompletedForMilestoneAsync(studentId, topicId, updatedMasteryScore, ct);
        if (!chapterCompleted)
        {
            return null;
        }

        return BuildChapterMilestone(chapterKey);
    }

    public async Task<CrossMathMilestoneDto?> CompleteMilestoneAsync(
        int studentId,
        string challengeKey,
        CancellationToken ct = default)
    {
        var normalizedKey = challengeKey.Trim().ToLowerInvariant();
        if (!ChallengeChapterMapper.ChapterKeys.Contains(normalizedKey) &&
            !string.Equals(normalizedKey, ChallengeChapterMapper.FinalMixedKey, StringComparison.Ordinal))
        {
            return null;
        }

        var row = await context.StudentChallengeProgress
            .FirstOrDefaultAsync(x => x.StudentId == studentId && x.ChallengeKey == normalizedKey, ct);
        if (row == null)
        {
            context.StudentChallengeProgress.Add(new StudentChallengeProgress
            {
                StudentId = studentId,
                ChallengeKey = normalizedKey,
                CompletedAtUtc = DateTime.UtcNow
            });
            await context.SaveChangesAsync(ct);
        }

        if (string.Equals(normalizedKey, ChallengeChapterMapper.FinalMixedKey, StringComparison.Ordinal))
        {
            return null;
        }

        var completedKeys = await context.StudentChallengeProgress
            .Where(x => x.StudentId == studentId)
            .Select(x => x.ChallengeKey)
            .ToListAsync(ct);

        var allChaptersCompleted = ChallengeChapterMapper.ChapterKeys.All(key => completedKeys.Contains(key));
        var finalAlreadyDone = completedKeys.Contains(ChallengeChapterMapper.FinalMixedKey);

        if (allChaptersCompleted && !finalAlreadyDone)
        {
            return BuildFinalMilestone();
        }

        return null;
    }

    private async Task<string?> ResolveChapterKeyFromTopicAsync(int topicId, CancellationToken ct)
    {
        var topicName = await context.Topics
            .Where(t => t.Id == topicId)
            .Select(t => t.Name)
            .FirstOrDefaultAsync(ct);

        if (topicName == null)
        {
            return null;
        }

        return ChallengeChapterMapper.FromTopicName(topicName);
    }

    private async Task<bool> IsTopicCompletedForMilestoneAsync(
        int studentId,
        int topicId,
        double updatedMasteryScore,
        CancellationToken ct)
    {
        if (updatedMasteryScore < TopicCompletionMasteryThreshold)
        {
            return false;
        }

        var uniqueQuestionsAttempted = await context.Attempts
            .Where(a => a.StudentId == studentId && a.Question.TopicId == topicId)
            .Select(a => a.QuestionId)
            .Distinct()
            .CountAsync(ct);

        return uniqueQuestionsAttempted >= TopicCompletionMinUniqueQuestions;
    }

    private static CrossMathMilestoneDto BuildChapterMilestone(string chapterKey)
    {
        var (title, mode) = chapterKey switch
        {
            "addition" => ("Addition Challenge", "addition"),
            "subtraction" => ("Subtraction Challenge", "subtraction"),
            "multiplication" => ("Multiplication Challenge", "multiplication"),
            "division" => ("Division Challenge", "division"),
            _ => ("Chapter Challenge", "addition")
        };

        return new CrossMathMilestoneDto
        {
            ChallengeKey = chapterKey,
            Title = title,
            Subtitle = "Fill the missing numbers and solve each row equation.",
            Mode = mode,
            Size = 4
        };
    }

    private static CrossMathMilestoneDto BuildFinalMilestone()
    {
        return new CrossMathMilestoneDto
        {
            ChallengeKey = ChallengeChapterMapper.FinalMixedKey,
            Title = "Grand CrossMath Final",
            Subtitle = "Mixed operations from all four chapters.",
            Mode = "mixed",
            Size = 6
        };
    }

    private async Task<bool> ShouldOfferFinalMilestoneAsync(int studentId, CancellationToken ct)
    {
        var completedKeys = await context.StudentChallengeProgress
            .Where(x => x.StudentId == studentId)
            .Select(x => x.ChallengeKey)
            .ToListAsync(ct);

        return ChallengeChapterMapper.ChapterKeys.All(key => completedKeys.Contains(key))
               && !completedKeys.Contains(ChallengeChapterMapper.FinalMixedKey);
    }
}
