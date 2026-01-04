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

        // Jednim query-em dohvati SVE potrebno
        var allAttempts = await context.Attempts
            .Where(a => a.StudentId == studentId && a.Question.TopicId == topicId)
            .OrderBy(a => a.CreatedAt)
            .Select(a => new { a.QuestionId, a.IsCorrect, a.CreatedAt })
            .ToListAsync(ct);

        var totalAttempts = allAttempts.Count;

        // Grupiraj po QuestionId - uzmi zadnji attempt
        var lastAttemptPerQuestion = allAttempts
            .GroupBy(a => a.QuestionId)
            .ToDictionary(
                g => g.Key,
                g => new
                {
                    LastAttempt = g.OrderByDescending(x => x.CreatedAt).First(),
                    Index = allAttempts.FindLastIndex(x => x.QuestionId == g.Key)
                });

        bool IsReady(int questionId)
        {
            if (!lastAttemptPerQuestion.ContainsKey(questionId))
                return true;

            var info = lastAttemptPerQuestion[questionId];
            var attemptsSince = totalAttempts - info.Index - 1;
            var requiredGap = info.LastAttempt.IsCorrect ? 45 : 10;

            return attemptsSince >= requiredGap;
        }

        // Dohvati SVA pitanja za topic (ne filtriraj po difficulty)
        var allQuestions = await context.Questions
            .Where(q => q.TopicId == topicId)
            .ToListAsync(ct);

        if (!allQuestions.Any())
            return null;

        // Filtriraj samo ready pitanja
        var readyQuestions = allQuestions
            .Where(q => IsReady(q.Id))
            .ToList();

        if (readyQuestions.Any())
        {
            // Preferiraj target difficulty ako postoji
            var preferredDifficulty = readyQuestions
                .Where(q => q.Difficulty == targetDifficulty)
                .ToList();

            if (preferredDifficulty.Any())
                return preferredDifficulty.OrderBy(q => Guid.NewGuid()).First();

            // Inače bilo koje ready pitanje
            return readyQuestions.OrderBy(q => Guid.NewGuid()).First();
        }

        // Emergency fallback: ako NIŠTA nije ready, uzmi najstarije
        return allQuestions
            .OrderBy(q => lastAttemptPerQuestion.ContainsKey(q.Id) 
                ? lastAttemptPerQuestion[q.Id].Index 
                : -1)
            .ThenBy(q => Guid.NewGuid())
            .First();
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
        var isCorrect = AnswerNormalizer.AreEquivalent(question.CorrectAnswer, answer);
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
