using AiAgents.MathTutorAgent.Domain.Entities;
using AiAgents.MathTutorAgent.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AiAgents.MathTutorAgent.Application.Services;

public class CurriculumService(MathTutorDbContext context)
{
    public async Task<Topic?> GetNextTopicAsync(int studentId, CancellationToken ct = default)
    {
        var completedTopics = await context.StudentTopicStates
            .Where(s => s.StudentId == studentId && s.MasteryScore >= 75)
            .Select(s => s.TopicId)
            .ToListAsync(ct);

        var availableTopics = await context.Topics
            .Where(t => !completedTopics.Contains(t.Id))
            .Include(t => t.Prerequisites)
            .ToListAsync(ct);

        foreach (var topic in availableTopics.OrderBy(t => t.DifficultyBand))
        {
            var prerequisites = topic.Prerequisites.Select(p => p.PrerequisiteTopicId).ToList();
            var allPrerequisitesMet = !prerequisites.Any() || prerequisites.All(p => completedTopics.Contains(p));

            if (allPrerequisitesMet)
                return topic;
        }

        return null;
    }
}