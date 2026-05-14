namespace AiAgents.MathTutorAgent.Application.Services;

public class QuestionDifficultyAdvisorService
{
    public int GetTargetDifficultyFromMastery(double masteryScore)
    {
        return masteryScore switch
        {
            < 30 => 1,
            < 60 => 2,
            < 85 => 3,
            _ => 4
        };
    }

    public int GetBaseTimeLimitSeconds(int difficulty)
    {
        return difficulty switch
        {
            <= 1 => 90,
            2 => 120,
            3 => 150,
            4 => 180,
            _ => 210
        };
    }

    public int AdaptDifficultyByPace(
        int baseDifficulty,
        IReadOnlyList<AttemptPaceSnapshot> orderedAttempts,
        ChallengePaceSignals? challengeSignals = null)
    {
        var adjustedDifficulty = baseDifficulty;

        if (challengeSignals is { TopicChapterChallengeCompleted: false } && challengeSignals.ChapterChallengesCompleted == 0)
        {
            adjustedDifficulty = Math.Max(1, adjustedDifficulty - 1);
        }

        if (challengeSignals is { TopicChapterChallengeCompleted: true })
        {
            adjustedDifficulty = Math.Min(5, adjustedDifficulty + 1);
        }

        if (orderedAttempts.Count < 5)
        {
            return adjustedDifficulty;
        }

        var recent = orderedAttempts.Skip(Math.Max(0, orderedAttempts.Count - 10)).ToList();
        var recentAccuracy = recent.Count(a => a.IsCorrect) / (double)recent.Count;
        var recentAvgTimeSec = recent.Average(a => a.TimeMs) / 1000.0;
        var expectedTimeSec = GetBaseTimeLimitSeconds(adjustedDifficulty);

        if (recentAccuracy >= 0.8 && recentAvgTimeSec < expectedTimeSec * 0.8)
        {
            return Math.Min(5, adjustedDifficulty + 1);
        }

        if (recentAccuracy < 0.45 || recentAvgTimeSec > expectedTimeSec * 1.25)
        {
            return Math.Max(1, adjustedDifficulty - 1);
        }

        return adjustedDifficulty;
    }
}

public sealed record AttemptPaceSnapshot(bool IsCorrect, int TimeMs);
public sealed record ChallengePaceSignals(int ChapterChallengesCompleted, bool TopicChapterChallengeCompleted, bool FinalChallengeCompleted);
