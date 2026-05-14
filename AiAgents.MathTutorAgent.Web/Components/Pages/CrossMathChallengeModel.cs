namespace AiAgents.MathTutorAgent.Web.Components.Pages;

public enum CrossMathOperationMode
{
    Addition,
    Subtraction,
    Multiplication,
    Division,
    Mixed
}

public sealed class CrossMathChallengeModel
{
    public required string ChallengeKey { get; init; }
    public required string Title { get; init; }
    public required string Subtitle { get; init; }
    public required CrossMathOperationMode Mode { get; init; }
    public required int Size { get; init; }
}
