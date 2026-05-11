using AiAgents.MathTutorAgent.Application.DTOs;
using AiAgents.MathTutorAgent.Domain.Entities;

namespace AiAgents.MathTutorAgent.Application.Services;

public class StudentInsightsCalculatorService
{
    public List<QuestionAttemptInsightDto> BuildQuestionAttemptInsights(List<Attempt> attempts)
    {
        return attempts
            .GroupBy(a => new
            {
                a.QuestionId,
                a.Question.QuestionText,
                TopicName = a.Question.Topic.Name,
                AreaName = a.Question.Topic.Area.ToString()
            })
            .Select(g =>
            {
                var ordered = g.OrderBy(x => x.CreatedAt).ToList();
                var solvedFromFirstTry = ordered.First().IsCorrect;
                return new QuestionAttemptInsightDto
                {
                    QuestionId = g.Key.QuestionId,
                    QuestionText = g.Key.QuestionText,
                    TopicName = g.Key.TopicName,
                    AreaName = g.Key.AreaName,
                    AttemptsCount = g.Count(),
                    SolvedFromFirstTry = solvedFromFirstTry,
                    AccuracyPercentage = g.Count(a => a.IsCorrect) * 100.0 / g.Count()
                };
            })
            .OrderByDescending(x => x.AttemptsCount)
            .ThenBy(x => x.AccuracyPercentage)
            .Take(12)
            .ToList();
    }

    public List<AreaAttemptInsightDto> BuildAreaAttemptInsights(List<Attempt> attempts)
    {
        return attempts
            .GroupBy(a => a.Question.Topic.Area)
            .Select(g =>
            {
                var byQuestion = g.GroupBy(x => x.QuestionId)
                    .Select(qg => qg.OrderBy(x => x.CreatedAt).ToList())
                    .ToList();

                var firstTryAreaRate = byQuestion.Count > 0
                    ? byQuestion.Count(q => q.First().IsCorrect) * 100.0 / byQuestion.Count
                    : 0;

                return new AreaAttemptInsightDto
                {
                    AreaName = g.Key.ToString(),
                    TotalAttempts = g.Count(),
                    UniqueQuestions = g.Select(x => x.QuestionId).Distinct().Count(),
                    AccuracyPercentage = g.Count(a => a.IsCorrect) * 100.0 / g.Count(),
                    FirstTrySuccessRate = firstTryAreaRate,
                    AverageAttemptsPerQuestion = g.Count() / (double)Math.Max(1, g.Select(x => x.QuestionId).Distinct().Count())
                };
            })
            .OrderByDescending(x => x.TotalAttempts)
            .ToList();
    }

    public double CalculateFirstTrySuccessRate(List<QuestionAttemptInsightDto> questionInsights)
    {
        return questionInsights.Count > 0
            ? questionInsights.Count(q => q.SolvedFromFirstTry) * 100.0 / questionInsights.Count
            : 0;
    }

    public int CalculateSuggestedDifficulty(double accuracy, double averageTimeSeconds)
    {
        var difficulty = accuracy switch
        {
            < 35 => 1,
            < 55 => 2,
            < 75 => 3,
            < 88 => 4,
            _ => 5
        };

        if (averageTimeSeconds > 90)
        {
            difficulty = Math.Max(1, difficulty - 1);
        }
        else if (averageTimeSeconds < 35)
        {
            difficulty = Math.Min(5, difficulty + 1);
        }

        return difficulty;
    }

    public string GetReadinessLabel(double accuracy, double averageTimeSeconds)
    {
        if (accuracy >= 85 && averageTimeSeconds <= 45)
        {
            return "Spreman za teze zadatke";
        }

        if (accuracy >= 65 && averageTimeSeconds <= 75)
        {
            return "Stabilan tempo";
        }

        return "Potrebno jos vjezbe";
    }
}
