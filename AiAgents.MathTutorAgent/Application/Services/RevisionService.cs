using AiAgents.MathTutorAgent.Domain.Entities;
using AiAgents.MathTutorAgent.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AiAgents.MathTutorAgent.Application.Services;

public class RevisionService(MathTutorDbContext context)
{
    public async Task<bool> ShouldInjectRevisionAsync(int studentId, CancellationToken ct = default)
    {
        var settings = await context.SystemSettings.FirstOrDefaultAsync(ct);
        var threshold = settings?.ForgettingRiskThreshold ?? 0.6;

        var hasHighRisk = await context.StudentTopicStates
            .AnyAsync(s => s.StudentId == studentId && s.ForgettingRisk >= threshold, ct);

        return hasHighRisk;
    }

    public async Task<Topic?> PickRevisionTopicAsync(int studentId, CancellationToken ct = default)
    {
        var state = await context.StudentTopicStates
            .Include(s => s.Topic)
            .Where(s => s.StudentId == studentId)
            .OrderByDescending(s => s.ForgettingRisk)
            .FirstOrDefaultAsync(ct);

        return state?.Topic;
    }

    public async Task UpdateScheduleAsync(int studentId, int topicId, CancellationToken ct = default)
    {
        var settings = await context.SystemSettings.AsNoTracking().FirstOrDefaultAsync(ct);
        var intervalDays = settings?.RevisionIntervalDays ?? 7;

        var schedule = await context.RevisionScheduleItems
            .FirstOrDefaultAsync(r => r.StudentId == studentId && r.TopicId == topicId, ct);

        if (schedule == null)
        {
            schedule = new RevisionScheduleItem
            {
                StudentId = studentId,
                TopicId = topicId,
                NextDueUtc = DateTime.UtcNow.AddDays(intervalDays)
            };
            context.RevisionScheduleItems.Add(schedule);
        }
        else
        {
            schedule.NextDueUtc = DateTime.UtcNow.AddDays(intervalDays);
        }

        await context.SaveChangesAsync(ct);
    }
}
