using AiAgents.MathTutorAgent.Domain.Entities;
using AiAgents.MathTutorAgent.Domain.Enums;
using AiAgents.MathTutorAgent.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AiAgents.MathTutorAgent.Application.Services;

public class WorkQueueService
{
    private readonly MathTutorDbContext _context;

    public WorkQueueService(MathTutorDbContext context)
    {
        _context = context;
    }

    public async Task<int> EnqueueAsync(WorkItem workItem, CancellationToken ct = default)
    {
        _context.WorkItems.Add(workItem);
        await _context.SaveChangesAsync(ct);
        return workItem.Id;
    }

    public async Task<WorkItem?> DequeueNextAsync(CancellationToken ct = default)
    {
        // ATOMIC CLAIM: Use raw SQL to prevent double-processing
        var workItem = await _context.WorkItems
            .FromSqlRaw(@"
                UPDATE TOP(1) WorkItems 
                SET Status = {0}, ProcessedAt = GETUTCDATE()
                OUTPUT INSERTED.*
                WHERE Status = {1}
                ORDER BY CreatedAt",
                (int)WorkStatus.Processing,
                (int)WorkStatus.Queued)
            .FirstOrDefaultAsync(ct);

        return workItem;
    }

    public async Task MarkDoneAsync(int workItemId, string resultJson, CancellationToken ct = default)
    {
        var workItem = await _context.WorkItems.FindAsync(new object[] { workItemId }, ct);
        if (workItem != null)
        {
            workItem.Status = WorkStatus.Done;
            workItem.ResultJson = resultJson;
            workItem.ProcessedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);
        }
    }

    public async Task MarkFailedAsync(int workItemId, string error, CancellationToken ct = default)
    {
        var workItem = await _context.WorkItems.FindAsync(new object[] { workItemId }, ct);
        if (workItem != null)
        {
            workItem.Status = WorkStatus.Failed;
            workItem.ResultJson = error;
            workItem.ProcessedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);
        }
    }

    public async Task<WorkItem?> GetWorkItemAsync(int workItemId, CancellationToken ct = default)
    {
        return await _context.WorkItems.FindAsync(new object[] { workItemId }, ct);
    }
}