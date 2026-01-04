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
        // ✅ FIX: Use CTE with UPDLOCK/READPAST for proper atomic claim
        // ORDER BY must be in the subquery/CTE, not after OUTPUT
        var results = await context.WorkItems
            .FromSqlRaw(@"
                ;WITH NextItem AS (
                    SELECT TOP(1) *
                    FROM WorkItems WITH (UPDLOCK, READPAST)
                    WHERE Status = {1}
                    ORDER BY CreatedAt
                )
                UPDATE NextItem
                SET Status = {0}, ProcessedAt = GETUTCDATE()
                OUTPUT INSERTED.*;",
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