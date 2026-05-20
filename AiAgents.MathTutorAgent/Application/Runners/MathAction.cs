using AiAgents.MathTutorAgent.Domain.Entities;

namespace AiAgents.MathTutorAgent.Application.Runners;

public record MathAction(
    WorkItem WorkItem,
    MathActionType Type,
    string? Reason = null);