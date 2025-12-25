using AiAgents.MathTutorAgent.Application.DTOs;
using AiAgents.MathTutorAgent.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AiAgents.MathTutorAgent.Application.Services;

public class StudentProfileService(MathTutorDbContext context)
{
    public async Task<StudentProfileDto> GetProfileAsync(int studentId, CancellationToken ct = default)
    {
        var student = await context.Students
            .Include(s => s.TopicStates).ThenInclude(ts => ts.Topic)
            .Include(s => s.Attempts).ThenInclude(a => a.Question)
            .FirstOrDefaultAsync(s => s.Id == studentId, ct);

        if (student == null)
            throw new KeyNotFoundException($"Student {studentId} not found");

        var totalAttempts = student.Attempts.Count;
        var correctAttempts = student.Attempts.Count(a => a.IsCorrect);
        var averageTime = student.Attempts.Any() 
            ? student.Attempts.Average(a => a.TimeMs) / 1000.0 
            : 0;

        var topicProgress = student.TopicStates
            .Select(ts => new TopicProgressDto
            {
                TopicName = ts.Topic.Name,
                MasteryScore = ts.MasteryScore,
                Confidence = ts.Confidence,
                LastPracticed = ts.LastPracticedUtc
            })
            .OrderByDescending(tp => tp.MasteryScore)
            .ToList();

        var recentActivity = student.Attempts
            .OrderByDescending(a => a.CreatedAt)
            .Take(10)
            .Select(a => new ActivityDto
            {
                Date = a.CreatedAt,
                QuestionText = a.Question.QuestionText,
                IsCorrect = a.IsCorrect,
                TimeSeconds = a.TimeMs / 1000.0
            })
            .ToList();

        return new StudentProfileDto
        {
            StudentId = student.Id,
            Name = student.Name,
            Email = student.Email,
            TotalAttempts = totalAttempts,
            CorrectAttempts = correctAttempts,
            AccuracyPercentage = totalAttempts > 0 ? (correctAttempts * 100.0 / totalAttempts) : 0,
            AverageTimeSeconds = averageTime,
            TopicProgress = topicProgress,
            RecentActivity = recentActivity
        };
    }

    public async Task<StudySessionStatsDto> GetStudySessionStatsAsync(int studentId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        var attempts = await context.Attempts
            .Include(a => a.Question).ThenInclude(q => q.Topic)
            .Where(a => a.StudentId == studentId && a.CreatedAt >= from && a.CreatedAt <= to)
            .OrderBy(a => a.CreatedAt)
            .ToListAsync(ct);

        var groupedByDate = attempts
            .GroupBy(a => a.CreatedAt.Date)
            .Select(g => new DailyStatsDto
            {
                Date = g.Key,
                TotalAttempts = g.Count(),
                CorrectAttempts = g.Count(a => a.IsCorrect),
                AverageTimeSeconds = g.Average(a => a.TimeMs) / 1000.0
            })
            .ToList();

        var groupedByTopic = attempts
            .GroupBy(a => a.Question.Topic.Name)
            .Select(g => new TopicStatsDto
            {
                TopicName = g.Key,
                TotalAttempts = g.Count(),
                CorrectAttempts = g.Count(a => a.IsCorrect),
                AccuracyPercentage = (g.Count(a => a.IsCorrect) * 100.0 / g.Count())
            })
            .OrderByDescending(t => t.AccuracyPercentage)
            .ToList();

        return new StudySessionStatsDto
        {
            FromDate = from,
            ToDate = to,
            DailyStats = groupedByDate,
            TopicStats = groupedByTopic
        };
    }
}