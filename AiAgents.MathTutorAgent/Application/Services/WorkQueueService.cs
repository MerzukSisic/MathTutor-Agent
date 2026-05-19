using AiAgents.MathTutorAgent.Domain.Entities;
using AiAgents.MathTutorAgent.Domain.Enums;
using AiAgents.MathTutorAgent.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AiAgents.MathTutorAgent.Application.Services;

public class WorkQueueService(MathTutorDbContext context)
{
    public async Task<int> EnqueueAsync(WorkItem workItem, CancellationToken ct = default)
    {
        context.WorkItems.Add(workItem);
        await context.SaveChangesAsync(ct);
        return workItem.Id;
    }

    /// <summary>
    /// Atomically claims and returns the next queued work item
    /// </summary>
    public async Task<WorkItem?> DequeueNextAsync(CancellationToken ct = default)
    {
        var results = await context.WorkItems
            .FromSqlRaw(@"
                WITH next_item AS (
                    SELECT ""Id""
                    FROM ""WorkItems""
                    WHERE ""Status"" = {1}
                    ORDER BY ""CreatedAt""
                    FOR UPDATE SKIP LOCKED
                    LIMIT 1
                )
                UPDATE ""WorkItems"" w
                SET ""Status"" = {0}, ""ProcessedAt"" = CURRENT_TIMESTAMP
                FROM next_item
                WHERE w.""Id"" = next_item.""Id""
                RETURNING w.*;",
                (int)WorkStatus.Processing,
                (int)WorkStatus.Queued)
            .ToListAsync(ct);

        return results.FirstOrDefault();
    }

    /// <summary>
    /// Idempotently marks work item as done - only updates if status is Processing
    /// </summary>
    public async Task MarkDoneAsync(int workItemId, string resultJson, CancellationToken ct = default)
    {
        var workItem = await context.WorkItems.FindAsync(new object[] { workItemId }, ct);
        if (workItem == null)
            return;

        // IDEMPOTENT: Only update if currently Processing
        // Done stays Done, Failed stays Failed
        if (workItem.Status != WorkStatus.Processing)
        {
            return; // Already finished or failed - no-op
        }

        workItem.Status = WorkStatus.Done;
        workItem.ResultJson = resultJson;
        workItem.ProcessedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Idempotently marks work item as failed - only updates if status is Processing
    /// </summary>
    public async Task MarkFailedAsync(int workItemId, string error, CancellationToken ct = default)
    {
        var workItem = await context.WorkItems.FindAsync(new object[] { workItemId }, ct);
        if (workItem == null)
            return;

        // IDEMPOTENT: Only update if currently Processing
        // Done stays Done (don't overwrite successful result)
        // Failed stays Failed (first error wins)
        if (workItem.Status != WorkStatus.Processing)
        {
            return; // Already finished or failed - no-op
        }

        workItem.Status = WorkStatus.Failed;
        workItem.ResultJson = error;
        workItem.ProcessedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);
    }

    public async Task<WorkItem?> GetWorkItemAsync(int workItemId, CancellationToken ct = default)
    {
        return await context.WorkItems.FindAsync(new object[] { workItemId }, ct);
    }
}
