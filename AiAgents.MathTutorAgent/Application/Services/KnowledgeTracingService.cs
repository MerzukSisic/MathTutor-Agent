using AiAgents.MathTutorAgent.Domain.Entities;
using AiAgents.MathTutorAgent.Domain.Enums;
using AiAgents.MathTutorAgent.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AiAgents.MathTutorAgent.Application.Services;

public class KnowledgeTracingService(MathTutorDbContext context)
{
    public async Task UpdateTopicStateAsync(int studentId, Attempt attempt, CancellationToken ct = default)
    {
        var question = await context.Questions.FindAsync(new object[] { attempt.QuestionId }, ct);
        if (question == null) return;

        var state = await context.StudentTopicStates
            .FirstOrDefaultAsync(s => s.StudentId == studentId && s.TopicId == question.TopicId, ct);

        if (state == null)
        {
            state = new StudentTopicState
            {
                StudentId = studentId,
                TopicId = question.TopicId,
                MasteryScore = 0,
                Confidence = 0.5,
                ForgettingRisk = 0,
                LastPracticedUtc = DateTime.UtcNow
            };
            context.StudentTopicStates.Add(state);
        }

        // ✅ FIX: Calculate forgetting risk BEFORE updating LastPracticedUtc
        var previousLastPracticed = state.LastPracticedUtc;
        var now = DateTime.UtcNow;
        var daysSinceLastPractice = (now - previousLastPracticed).TotalDays;
        
        // Update mastery score (simplified ELO-like)
        if (attempt.IsCorrect)
        {
            state.MasteryScore = Math.Min(100, state.MasteryScore + 10);
            state.Confidence = Math.Min(1.0, state.Confidence + 0.05);
        }
        else
        {
            state.MasteryScore = Math.Max(0, state.MasteryScore - 5);
            state.Confidence = Math.Max(0.0, state.Confidence - 0.05);
        }

        // Calculate forgetting risk based on time elapsed
        state.ForgettingRisk = Math.Min(1.0, daysSinceLastPractice / 7.0); // 7 days = full risk

        // NOW update LastPracticedUtc
        state.LastPracticedUtc = now;

        await context.SaveChangesAsync(ct);
    }

    public async Task<double> GetMasteryScoreAsync(int studentId, int topicId, CancellationToken ct = default)
    {
        var state = await context.StudentTopicStates
            .FirstOrDefaultAsync(s => s.StudentId == studentId && s.TopicId == topicId, ct);

        return state?.MasteryScore ?? 0;
    }

    public async Task<StudentTopicState?> GetTopicStateAsync(int studentId, int topicId, CancellationToken ct = default)
    {
        return await context.StudentTopicStates
            .FirstOrDefaultAsync(s => s.StudentId == studentId && s.TopicId == topicId, ct);
    }
}