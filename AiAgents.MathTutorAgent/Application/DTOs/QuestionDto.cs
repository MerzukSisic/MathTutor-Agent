using AiAgents.MathTutorAgent.Domain.Entities;

namespace AiAgents.MathTutorAgent.Application.DTOs;

public class QuestionDto
{
    public int Id { get; set; }
    public int TopicId { get; set; }
    public int Difficulty { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string CorrectAnswer { get; set; } = string.Empty;
    public string? SolutionSteps { get; set; }
    public List<string> CommonMistakes { get; set; } = new();

    // Navigation property
    public Topic Topic { get; set; } = null!;
}