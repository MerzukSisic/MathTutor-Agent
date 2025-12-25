using AiAgents.MathTutorAgent.Application.DTOs;
using AiAgents.MathTutorAgent.Domain.Entities;
using AiAgents.MathTutorAgent.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AiAgents.MathTutorAgent.Application.Services;

public class AdminService
{
    private readonly MathTutorDbContext _context;

    public AdminService(MathTutorDbContext context)
    {
        _context = context;
    }

    // ========== QUESTIONS ==========
    public async Task<List<AdminQuestionDto>> GetAllQuestionsAsync(CancellationToken ct = default)
    {
        return await _context.Questions
            .Include(q => q.Topic)
            .Select(q => new AdminQuestionDto
            {
                Id = q.Id,
                TopicId = q.TopicId,
                TopicName = q.Topic.Name,
                Difficulty = q.Difficulty,
                QuestionText = q.QuestionText,
                CorrectAnswer = q.CorrectAnswer
            })
            .ToListAsync(ct);
    }

    public async Task<AdminQuestionDto> CreateQuestionAsync(CreateQuestionDto dto, CancellationToken ct = default)
    {
        var question = new Question
        {
            TopicId = dto.TopicId,
            Difficulty = dto.Difficulty,
            QuestionText = dto.QuestionText,
            CorrectAnswer = dto.CorrectAnswer,
            SolutionSteps = dto.SolutionSteps,
            CommonMistakes = dto.CommonMistakes ?? new List<string>()
        };

        _context.Questions.Add(question);
        await _context.SaveChangesAsync(ct);

        await _context.Entry(question).Reference(q => q.Topic).LoadAsync(ct);

        return new AdminQuestionDto
        {
            Id = question.Id,
            TopicId = question.TopicId,
            TopicName = question.Topic.Name,
            Difficulty = question.Difficulty,
            QuestionText = question.QuestionText,
            CorrectAnswer = question.CorrectAnswer
        };
    }

    public async Task<AdminQuestionDto> UpdateQuestionAsync(int id, CreateQuestionDto dto, CancellationToken ct = default)
    {
        var question = await _context.Questions
            .Include(q => q.Topic)
            .FirstOrDefaultAsync(q => q.Id == id, ct);

        if (question == null)
            throw new InvalidOperationException($"Question {id} not found");

        question.TopicId = dto.TopicId;
        question.Difficulty = dto.Difficulty;
        question.QuestionText = dto.QuestionText;
        question.CorrectAnswer = dto.CorrectAnswer;
        question.SolutionSteps = dto.SolutionSteps;
        question.CommonMistakes = dto.CommonMistakes ?? new List<string>();

        await _context.SaveChangesAsync(ct);

        return new AdminQuestionDto
        {
            Id = question.Id,
            TopicId = question.TopicId,
            TopicName = question.Topic.Name,
            Difficulty = question.Difficulty,
            QuestionText = question.QuestionText,
            CorrectAnswer = question.CorrectAnswer
        };
    }

    public async Task DeleteQuestionAsync(int id, CancellationToken ct = default)
    {
        var question = await _context.Questions.FindAsync(new object[] { id }, ct);
        if (question == null)
            throw new InvalidOperationException($"Question {id} not found");

        _context.Questions.Remove(question);
        await _context.SaveChangesAsync(ct);
    }

    // ========== TOPICS ==========
    public async Task<List<TopicDto>> GetAllTopicsAsync(CancellationToken ct = default)
    {
        return await _context.Topics
            .Select(t => new TopicDto
            {
                Id = t.Id,
                Name = t.Name,
                Area = t.Area.ToString(),
                DifficultyBand = t.DifficultyBand
            })
            .ToListAsync(ct);
    }

    // ========== STUDENTS ==========
    public async Task<List<StudentDto>> GetAllStudentsAsync(CancellationToken ct = default)
    {
        return await _context.Students
            .Select(s => new StudentDto
            {
                Id = s.Id,
                Name = s.Name,
                Email = s.Email,
                CreatedAt = s.CreatedAt
            })
            .ToListAsync(ct);
    }

    // ========== METRICS ==========
    public async Task<PerformanceMetricsDto> GetPerformanceMetricsAsync(CancellationToken ct = default)
    {
        var totalStudents = await _context.Students.CountAsync(ct);
        var totalQuestions = await _context.Questions.CountAsync(ct);
        var totalAttempts = await _context.Attempts.CountAsync(ct);

        var workItemStats = await _context.WorkItems
            .GroupBy(w => w.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var successCount = workItemStats.FirstOrDefault(s => s.Status == Domain.Enums.WorkStatus.Done)?.Count ?? 0;
        var totalWorkItems = workItemStats.Sum(s => s.Count);
        var successRate = totalWorkItems > 0 ? (double)successCount / totalWorkItems * 100 : 0;

        var avgProcessingTime = await _context.WorkItems
            .Where(w => w.ProcessedAt != null)
            .Select(w => EF.Functions.DateDiffMillisecond(w.CreatedAt, w.ProcessedAt!.Value))
            .AverageAsync(ct);

        return new PerformanceMetricsDto
        {
            TotalStudents = totalStudents,
            TotalQuestions = totalQuestions,
            TotalAttempts = totalAttempts,
            WorkItemSuccessRate = successRate,
            AverageProcessingTimeMs = avgProcessingTime
        };
    }
}