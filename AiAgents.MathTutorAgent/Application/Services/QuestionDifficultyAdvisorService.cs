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

    public int AdaptDifficultyByPace(int baseDifficulty, IReadOnlyList<AttemptPaceSnapshot> orderedAttempts)
    {
        if (orderedAttempts.Count < 5)
        {
            return baseDifficulty;
        }

        var recent = orderedAttempts.Skip(Math.Max(0, orderedAttempts.Count - 10)).ToList();
        var recentAccuracy = recent.Count(a => a.IsCorrect) / (double)recent.Count;
        var recentAvgTimeSec = recent.Average(a => a.TimeMs) / 1000.0;
        var expectedTimeSec = GetBaseTimeLimitSeconds(baseDifficulty);

        if (recentAccuracy >= 0.8 && recentAvgTimeSec < expectedTimeSec * 0.8)
        {
            return Math.Min(5, baseDifficulty + 1);
        }

        if (recentAccuracy < 0.45 || recentAvgTimeSec > expectedTimeSec * 1.25)
        {
            return Math.Max(1, baseDifficulty - 1);
        }

        return baseDifficulty;
    }
}

public sealed record AttemptPaceSnapshot(bool IsCorrect, int TimeMs);
