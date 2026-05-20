namespace AiAgents.MathTutorAgent.Domain.Entities;

public class Question
{
    public int Id { get; set; }
    public int TopicId { get; set; }
    public int Difficulty { get; set; } // 1-5
    public string QuestionText { get; set; } = string.Empty;
    public string CorrectAnswer { get; set; } = string.Empty;
    public string? SolutionSteps { get; set; }
    public List<string> CommonMistakes { get; set; } = new();

    // Navigation
    public Topic Topic { get; set; } = null!;
    public List<Attempt> Attempts { get; set; } = new();
}