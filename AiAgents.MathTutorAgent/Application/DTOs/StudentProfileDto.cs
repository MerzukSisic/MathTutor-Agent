namespace AiAgents.MathTutorAgent.Application.DTOs;

public class StudentProfileDto
{
    public int StudentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int TotalAttempts { get; set; }
    public int CorrectAttempts { get; set; }
    public double AccuracyPercentage { get; set; }
    public double AverageTimeSeconds { get; set; }
    public double FirstTrySuccessRate { get; set; }
    public string ReadinessLabel { get; set; } = string.Empty;
    public int SuggestedDifficulty { get; set; }
    public List<TopicProgressDto> TopicProgress { get; set; } = new();
    public List<ActivityDto> RecentActivity { get; set; } = new();
    public List<QuestionAttemptInsightDto> QuestionAttemptInsights { get; set; } = new();
    public List<AreaAttemptInsightDto> AreaAttemptInsights { get; set; } = new();
}

public class TopicProgressDto
{
    public string TopicName { get; set; } = string.Empty;
    public string AreaName { get; set; } = string.Empty;
    public double MasteryScore { get; set; }
    public double Confidence { get; set; }
    public DateTime LastPracticed { get; set; }
}

public class ActivityDto
{
    public DateTime Date { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string TopicName { get; set; } = string.Empty;
    public string AreaName { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public double TimeSeconds { get; set; }
}

public class StudySessionStatsDto
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public List<DailyStatsDto> DailyStats { get; set; } = new();
    public List<TopicStatsDto> TopicStats { get; set; } = new();
}

public class DailyStatsDto
{
    public DateTime Date { get; set; }
    public int TotalAttempts { get; set; }
    public int CorrectAttempts { get; set; }
    public double AverageTimeSeconds { get; set; }
}

public class TopicStatsDto
{
    public string TopicName { get; set; } = string.Empty;
    public int TotalAttempts { get; set; }
    public int CorrectAttempts { get; set; }
    public double AccuracyPercentage { get; set; }
}

public class QuestionAttemptInsightDto
{
    public int QuestionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string TopicName { get; set; } = string.Empty;
    public string AreaName { get; set; } = string.Empty;
    public int AttemptsCount { get; set; }
    public bool SolvedFromFirstTry { get; set; }
    public double AccuracyPercentage { get; set; }
}

public class AreaAttemptInsightDto
{
    public string AreaName { get; set; } = string.Empty;
    public int TotalAttempts { get; set; }
    public int UniqueQuestions { get; set; }
    public double AccuracyPercentage { get; set; }
    public double FirstTrySuccessRate { get; set; }
    public double AverageAttemptsPerQuestion { get; set; }
}
