using AiAgents.MathTutorAgent.Domain.Entities;
using AiAgents.MathTutorAgent.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AiAgents.MathTutorAgent.Application.Services;

public class CurriculumService(MathTutorDbContext context)
{
    public async Task<Topic?> GetNextTopicAsync(int studentId, CancellationToken ct = default)
    {
        var topicStates = await context.StudentTopicStates
            .Where(s => s.StudentId == studentId)
            .Select(s => new { s.TopicId, s.MasteryScore })
            .ToListAsync(ct);

        var completedTopics = topicStates
            .Where(s => s.MasteryScore >= 75)
            .Select(s => s.TopicId)
            .ToHashSet();

        var availableTopics = await context.Topics
            .Where(t => !completedTopics.Contains(t.Id))
            .Include(t => t.Prerequisites)
            .ToListAsync(ct);

        var unlockedTopics = availableTopics
            .Where(t =>
            {
                var prerequisites = t.Prerequisites.Select(p => p.PrerequisiteTopicId).ToList();
                return prerequisites.Count == 0 || prerequisites.All(p => completedTopics.Contains(p));
            })
            .ToList();

        if (unlockedTopics.Count == 0)
        {
            return null;
        }

        var attemptsByTopic = await context.Attempts
            .Where(a => a.StudentId == studentId)
            .GroupBy(a => a.Question.TopicId)
            .Select(g => new { TopicId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var masteryByTopic = topicStates.ToDictionary(x => x.TopicId, x => x.MasteryScore);
        var attemptsCountByTopic = attemptsByTopic.ToDictionary(x => x.TopicId, x => x.Count);

        var minDifficultyBand = unlockedTopics.Min(t => t.DifficultyBand);
        var candidateTopics = unlockedTopics
            .Where(t => t.DifficultyBand <= minDifficultyBand + 1)
            .ToList();

        return candidateTopics
            .OrderBy(t => masteryByTopic.GetValueOrDefault(t.Id, 0))
            .ThenBy(t => attemptsCountByTopic.GetValueOrDefault(t.Id, 0))
            .ThenBy(t => t.DifficultyBand)
            .ThenBy(_ => Guid.NewGuid())
            .FirstOrDefault();
    }
}
