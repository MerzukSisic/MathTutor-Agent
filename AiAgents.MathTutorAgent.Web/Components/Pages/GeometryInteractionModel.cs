namespace AiAgents.MathTutorAgent.Web.Components.Pages;

public sealed class GeometryInteractionModel
{
    public required string ShapeKey { get; init; }
    public required string ShapeLabel { get; init; }
    public required string CountTarget { get; init; }
    public required int ElementCount { get; init; }
    public required string QuestionText { get; init; }
}
