using AiAgents.Core;
using AiAgents.MathTutorAgent.Domain.Entities;
using AiAgents.MathTutorAgent.Domain.Enums;

namespace AiAgents.MathTutorAgent.Application.Runners;

public class MathTutoringPolicy : IPolicy<WorkItem, MathAction>
{
    public Task<MathAction> DecideAsync(WorkItem percept, CancellationToken ct)
    {
        // GUARD RAILS: Validate payload exists for actions that need it
        var needsPayload = percept.Type == WorkItemType.SubmitAnswer
                           || percept.Type == WorkItemType.Explain
                           || percept.Type == WorkItemType.UploadImage;

        if (needsPayload && string.IsNullOrWhiteSpace(percept.PayloadJson))
        {
            return Task.FromResult(new MathAction(
                percept,
                MathActionType.RejectInvalid,
                "Missing required payload"));
        }

        // THINK: Map WorkItemType to MathActionType (could add more logic here)
        var actionType = percept.Type switch
        {
            WorkItemType.NextQuestion => MathActionType.NextQuestion,
            WorkItemType.SubmitAnswer => MathActionType.SubmitAnswer,
            WorkItemType.Explain => MathActionType.Explain,
            WorkItemType.UploadImage => MathActionType.UploadImage,
            _ => MathActionType.RejectInvalid
        };

        return Task.FromResult(new MathAction(percept, actionType));
    }
}