using AiAgents.MathTutorAgent.Domain.Enums;

namespace AiAgents.MathTutorAgent.Application.DTOs;

public class MathTickResult
{
    public int WorkItemId { get; set; }
    public WorkItemType Type { get; set; }
    public int StudentId { get; set; }
    public TickOutcome Outcome { get; set; } // ✅ ENUM umjesto string
    public int? TopicId { get; set; }
    public AdvanceDecision? Decision { get; set; }
    public double? MasteryDelta { get; set; }
    public object? UiPayload { get; set; }
    public List<ReferenceDto>? References { get; set; }
}

// ✅ DODAJ ENUM
public enum TickOutcome
{
    NoWork,
    QuestionGenerated,
    AnswerEvaluated,
    ExplanationReady,
    ImageIndexed,
    NoTopicsAvailable,
    NoQuestionsAvailable,
    ValidationFailed,
    Failed
}

public class ReferenceDto
{
    public string Type { get; set; } = string.Empty;
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string PageOrTime { get; set; } = string.Empty;
}