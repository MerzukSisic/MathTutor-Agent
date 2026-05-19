using AiAgents.Core;
using AiAgents.MathTutorAgent.Application.Services;
using AiAgents.MathTutorAgent.Domain.Entities;

namespace AiAgents.MathTutorAgent.Application.Runners;

public class WorkQueuePerceptionSource(WorkQueueService queueService) : IPerceptionSource<WorkItem>
{
    public async Task<WorkItem?> GetNextPerceptAsync(CancellationToken cancellationToken)
    {
        return await queueService.DequeueNextAsync(cancellationToken);
    }
}
