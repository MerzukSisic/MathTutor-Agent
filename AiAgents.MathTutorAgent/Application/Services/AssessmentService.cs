using AiAgents.MathTutorAgent.Domain.Entities;
using AiAgents.MathTutorAgent.Domain.Enums;
using AiAgents.MathTutorAgent.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AiAgents.MathTutorAgent.Application.Services;

public class AssessmentService(
    MathTutorDbContext context,
    QuestionSelectionService questionSelectionService,
    QuestionTimeLimitService questionTimeLimitService)
{
    public async Task<Question?> SelectNextQuestionAsync(int studentId, int topicId, CancellationToken ct = default)
    {
        var state = await context.StudentTopicStates
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.StudentId == studentId && s.TopicId == topicId, ct);

        var masteryScore = state?.MasteryScore ?? 0;

        return await questionSelectionService.SelectNextQuestionAsync(
            studentId,
            topicId,
            masteryScore,
            ct);
    }

    public async Task<Question?> GetQuestionAsync(int questionId, CancellationToken ct = default)
    {
        return await context.Questions
            .Include(q => q.Topic)
            .FirstOrDefaultAsync(q => q.Id == questionId, ct);
    }

    public Task<bool> EvaluateAnswerAsync(Question question, string answer, CancellationToken ct = default)
    {
        var isCorrect = AnswerNormalizer.AreEquivalent(question.CorrectAnswer, answer);
        return Task.FromResult(isCorrect);
    }

    public Task<int> GetTimeLimitSecondsAsync(int studentId, Question question, CancellationToken ct = default)
    {
        return questionTimeLimitService.GetTimeLimitSecondsAsync(studentId, question, ct);
    }

    public async Task<Attempt> SaveAttemptAsync(
        int studentId,
        int questionId,
        bool isCorrect,
        int timeMs,
        string answerRaw,
        List<string>? errorTagsDetected = null,
        CancellationToken ct = default)
    {
        var attempt = new Attempt
        {
            StudentId = studentId,
            QuestionId = questionId,
            IsCorrect = isCorrect,
            TimeMs = timeMs,
            AnswerRaw = answerRaw,
            ErrorTagsDetected = errorTagsDetected ?? new List<string>(),
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
        {
            return AdvanceDecision.Remediate;
        }

        if (state.MasteryScore < 85)
        {
            return AdvanceDecision.Review;
        }

        return AdvanceDecision.Advance;
    }

    public async Task<bool> IsFirstQuestionInTopicForStudentAsync(int studentId, int topicId, CancellationToken ct = default)
    {
        return !await context.Attempts.AnyAsync(
            a => a.StudentId == studentId && a.Question.TopicId == topicId,
            ct);
    }
}
