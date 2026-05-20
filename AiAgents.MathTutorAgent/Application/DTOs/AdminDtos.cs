namespace AiAgents.MathTutorAgent.Application.DTOs;

public record AdminQuestionDto
{
    public int Id { get; init; }
    public int TopicId { get; init; }
    public string TopicName { get; init; } = string.Empty;
    public int Difficulty { get; init; }
    public string QuestionText { get; init; } = string.Empty;
    public string CorrectAnswer { get; init; } = string.Empty;
}

public record CreateQuestionDto
{
    public int TopicId { get; init; }
    public int Difficulty { get; init; }
    public string QuestionText { get; init; } = string.Empty;
    public string CorrectAnswer { get; init; } = string.Empty;
    public string? SolutionSteps { get; init; }
    public List<string>? CommonMistakes { get; init; }
}

public record TopicDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Area { get; init; } = string.Empty;
    public int DifficultyBand { get; init; }
}

public record StudentDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

public record CreateStudentDto
{
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
}

public record CreateStudentResultDto
{
    public StudentDto Student { get; init; } = new();
    public bool AccountCreated { get; init; }
    public bool InviteSent { get; init; }
    public string? InviteLink { get; init; }
}

public record UpdateStudentDto
{
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
}

public record PerformanceMetricsDto
{
    public int TotalStudents { get; init; }
    public int TotalQuestions { get; init; }
    public int TotalAttempts { get; init; }

    // DODANO za Admin.razor:
    public int TotalWorkItems { get; init; }
    public int CompletedWorkItems { get; init; }
    public double SuccessRate { get; init; }  // Renamed from WorkItemSuccessRate

    public double AverageProcessingTimeMs { get; init; }
}
