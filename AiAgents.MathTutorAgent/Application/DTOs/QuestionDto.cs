namespace AiAgents.MathTutorAgent.Application.DTOs;

public class QuestionDto
{
    public int Id { get; set; }
    public int TopicId { get; set; }
    public string TopicName { get; set; } = string.Empty;
    public string AreaName { get; set; } = string.Empty;
    public int Difficulty { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public int TimeLimitSeconds { get; set; }
    public bool IsFirstQuestionInTopicForStudent { get; set; }
    public string CorrectAnswer { get; set; } = string.Empty;
    public string? SolutionSteps { get; set; }
    public List<string> CommonMistakes { get; set; } = new();
}
