using AiAgents.MathTutorAgent.Application.DTOs;
using AiAgents.MathTutorAgent.Domain.Enums;
using AiAgents.MathTutorAgent.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AiAgents.MathTutorAgent.Application.Services;

public class StudentProfileService(
    MathTutorDbContext context,
    StudentInsightsCalculatorService insightsCalculator,
    MathContentLocalizationService localizationService)
{
    public async Task<StudentProfileDto> GetProfileAsync(int studentId, string? languageCode = null, CancellationToken ct = default)
    {
        var language = localizationService.NormalizeLanguage(languageCode);
        var student = await context.Students
            .AsNoTracking()
            .AsSplitQuery()
            .Include(s => s.TopicStates).ThenInclude(ts => ts.Topic)
            .Include(s => s.Attempts).ThenInclude(a => a.Question).ThenInclude(q => q.Topic)
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
                TopicName = localizationService.LocalizeTopicName(ts.Topic.Name, language),
                AreaName = localizationService.LocalizeAreaName(ts.Topic.Area.ToString(), language),
                MasteryScore = ts.MasteryScore,
                Confidence = ts.Confidence,
                LastPracticed = ts.LastPracticedUtc
            })
            .OrderByDescending(tp => tp.MasteryScore)
            .ToList();

        var recentActivity = student.Attempts
            .OrderByDescending(a => a.CreatedAt)
            .Take(500)
            .Select(a => new ActivityDto
            {
                Date = a.CreatedAt,
                QuestionText = localizationService.LocalizeQuestionText(a.Question.QuestionText, language),
                TopicName = localizationService.LocalizeTopicName(a.Question.Topic.Name, language),
                AreaName = localizationService.LocalizeAreaName(a.Question.Topic.Area.ToString(), language),
                SubmittedAnswer = localizationService.LocalizeAnswerToken(a.AnswerRaw, language),
                CorrectAnswer = localizationService.LocalizeAnswerToken(a.Question.CorrectAnswer, language),
                IsCorrect = a.IsCorrect,
                TimeSeconds = a.TimeMs / 1000.0
            })
            .ToList();

        var questionGroups = insightsCalculator.BuildQuestionAttemptInsights(student.Attempts);
        var firstTrySuccessRate = insightsCalculator.CalculateFirstTrySuccessRate(questionGroups);
        var areaInsights = insightsCalculator.BuildAreaAttemptInsights(student.Attempts);

        foreach (var insight in questionGroups)
        {
            insight.QuestionText = localizationService.LocalizeQuestionText(insight.QuestionText, language);
            insight.TopicName = localizationService.LocalizeTopicName(insight.TopicName, language);
            insight.AreaName = localizationService.LocalizeAreaName(insight.AreaName, language);
        }

        foreach (var areaInsight in areaInsights)
        {
            areaInsight.AreaName = localizationService.LocalizeAreaName(areaInsight.AreaName, language);
        }

        var accuracy = totalAttempts > 0 ? correctAttempts * 100.0 / totalAttempts : 0;
        var suggestedDifficulty = insightsCalculator.CalculateSuggestedDifficulty(accuracy, averageTime);
        var readinessLabel = insightsCalculator.GetReadinessLabel(accuracy, averageTime);

        return new StudentProfileDto
        {
            StudentId = student.Id,
            Name = student.Name,
            Email = student.Email,
            TotalAttempts = totalAttempts,
            CorrectAttempts = correctAttempts,
            AccuracyPercentage = accuracy,
            AverageTimeSeconds = averageTime,
            FirstTrySuccessRate = firstTrySuccessRate,
            SuggestedDifficulty = suggestedDifficulty,
            ReadinessLabel = language == "bs"
                ? readinessLabel
                : readinessLabel switch
                {
                    "Spreman za teze zadatke" => "Ready for harder tasks",
                    "Stabilan tempo" => "Stable pace",
                    "Potrebno jos vjezbe" => "Needs more practice",
                    _ => readinessLabel
                },
            TopicProgress = topicProgress,
            RecentActivity = recentActivity,
            QuestionAttemptInsights = questionGroups,
            AreaAttemptInsights = areaInsights
        };
    }

    public async Task<StudySessionStatsDto> GetStudySessionStatsAsync(int studentId, DateTime from, DateTime to, string? languageCode = null, CancellationToken ct = default)
    {
        var language = localizationService.NormalizeLanguage(languageCode);
        var attempts = await context.Attempts
            .AsNoTracking()
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
                TopicName = localizationService.LocalizeTopicName(g.Key, language),
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
    public async Task<DashboardStatsDto> GetDashboardStatsAsync(CancellationToken ct = default)
    {
        // Total Students
        var totalStudents = await context.Students.CountAsync(ct);

        // Active Sessions - studenti koji imaju aktivne work items
        var activeSessions = await context.WorkItems
            .Where(w => w.Status == WorkStatus.Processing || w.Status == WorkStatus.Queued)
            .Select(w => w.StudentId)
            .Distinct()
            .CountAsync(ct);

        // Questions Completed - svi tačno odgovoreni pokušaji (svi studenti)
        var questionsCompleted = await context.Attempts
            .CountAsync(a => a.IsCorrect, ct);

        // Agent Status - da li ima aktivnih work items
        var hasActiveWork = await context.WorkItems
            .AnyAsync(w => w.Status == WorkStatus.Processing, ct);

        return new DashboardStatsDto
        {
            TotalStudents = totalStudents,
            ActiveSessions = activeSessions,
            QuestionsCompleted = questionsCompleted,
            AgentStatus = hasActiveWork ? "Processing" : "Online"
        };
    }
}
