namespace AiAgents.MathTutorAgent.Application.Services;

public static class ChallengeChapterMapper
{
    public static readonly string[] ChapterKeys = ["addition", "subtraction", "multiplication", "division"];
    public const string FinalMixedKey = "final-mixed";

    public static string? FromTopicName(string topicName)
    {
        if (topicName.Contains("addition", StringComparison.OrdinalIgnoreCase)) return "addition";
        if (topicName.Contains("subtraction", StringComparison.OrdinalIgnoreCase)) return "subtraction";
        if (topicName.Contains("multiplication", StringComparison.OrdinalIgnoreCase)) return "multiplication";
        if (topicName.Contains("division", StringComparison.OrdinalIgnoreCase)) return "division";
        return null;
    }

    public static int CountCompletedChapterChallenges(IEnumerable<string> keys)
    {
        var set = keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return ChapterKeys.Count(set.Contains);
    }
}
