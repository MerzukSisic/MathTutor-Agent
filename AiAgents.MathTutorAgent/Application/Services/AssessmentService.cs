using AiAgents.MathTutorAgent.Domain.Entities;
using AiAgents.MathTutorAgent.Domain.Enums;
using AiAgents.MathTutorAgent.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AiAgents.MathTutorAgent.Application.Services;

public class AssessmentService(MathTutorDbContext context)
{
    public async Task<Question?> SelectNextQuestionAsync(int studentId, int topicId, CancellationToken ct = default)
    {
        var state = await context.StudentTopicStates
            .FirstOrDefaultAsync(s => s.StudentId == studentId && s.TopicId == topicId, ct);

        var targetDifficulty = state?.MasteryScore switch
        {
            null or < 30 => 1,
            < 60 => 2,
            < 85 => 3,
            _ => 4
        };

        var question = await context.Questions
            .Where(q => q.TopicId == topicId && q.Difficulty == targetDifficulty)
            .OrderBy(q => Guid.NewGuid()) // Random selection
            .FirstOrDefaultAsync(ct);

        return question;
    }

    public async Task<Question?> GetQuestionAsync(int questionId, CancellationToken ct = default)
    {
        return await context.Questions
            .Include(q => q.Topic)
            .FirstOrDefaultAsync(q => q.Id == questionId, ct);
    }

    public Task<bool> EvaluateAnswerAsync(Question question, string answer, CancellationToken ct = default)
    {
        // Simple exact match for MVP
        var isCorrect = string.Equals(
            question.CorrectAnswer.Trim(), 
            answer.Trim(), 
            StringComparison.OrdinalIgnoreCase);

        return Task.FromResult(isCorrect);
    }

    public async Task<Attempt> SaveAttemptAsync(
        int studentId,
        int questionId,
        bool isCorrect,
        int timeMs,
        string answerRaw,
        CancellationToken ct = default)
    {
        var attempt = new Attempt
        {
            StudentId = studentId,
            QuestionId = questionId,
            IsCorrect = isCorrect,
            TimeMs = timeMs,
            AnswerRaw = answerRaw,
            ErrorTagsDetected = new List<string>(),
            CreatedAt = DateTime.UtcNow
        };

        context.Attempts.Add(attempt);
        await context.SaveChangesAsync(ct);

        return attempt;
    }

    public async Task<AdvanceDecision> DetermineAdvanceDecisionAsync(
        int studentId, 
        int topicId, 
        CancellationToken ct = default)
    {
        var state = await context.StudentTopicStates
            .FirstOrDefaultAsync(s => s.StudentId == studentId && s.TopicId == topicId, ct);

        if (state == null || state.MasteryScore < 60)
            return AdvanceDecision.Remediate;

        if (state.MasteryScore < 85)
            return AdvanceDecision.Review;

        return AdvanceDecision.Advance;
    }
}