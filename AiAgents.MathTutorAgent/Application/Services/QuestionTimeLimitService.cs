using AiAgents.MathTutorAgent.Domain.Entities;
using AiAgents.MathTutorAgent.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AiAgents.MathTutorAgent.Application.Services;

public class QuestionTimeLimitService(
    MathTutorDbContext context,
    QuestionDifficultyAdvisorService difficultyAdvisor)
{
    public async Task<int> GetTimeLimitSecondsAsync(int studentId, Question question, CancellationToken ct = default)
    {
        var baseLimit = difficultyAdvisor.GetBaseTimeLimitSeconds(question.Difficulty);

        var recentAttempts = await context.Attempts
            .Where(a => a.StudentId == studentId && a.Question.TopicId == question.TopicId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(10)
            .ToListAsync(ct);

        if (recentAttempts.Count == 0)
        {
            return baseLimit;
        }

        var avgTimeSec = recentAttempts.Average(a => a.TimeMs) / 1000.0;
        var recentAccuracy = recentAttempts.Count(a => a.IsCorrect) / (double)recentAttempts.Count;

        var paceFactor = recentAccuracy >= 0.8 && avgTimeSec < baseLimit * 0.8
            ? 0.9
            : recentAccuracy < 0.5 || avgTimeSec > baseLimit * 1.2
                ? 1.2
                : 1.0;

        var personalized = (int)Math.Round(baseLimit * paceFactor);
        return Math.Clamp(personalized, 45, 300);
    }
}
