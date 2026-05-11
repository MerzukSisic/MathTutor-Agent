using AiAgents.MathTutorAgent.Application.DTOs;
using AiAgents.MathTutorAgent.Domain.Entities;
using AiAgents.MathTutorAgent.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AiAgents.MathTutorAgent.Application.Services;

public class AdminService(MathTutorDbContext context)
{
    // ========== QUESTIONS ==========
    public async Task<List<AdminQuestionDto>> GetAllQuestionsAsync(CancellationToken ct = default)
    {
        return await context.Questions
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

        context.Questions.Add(question);
        await context.SaveChangesAsync(ct);

        await context.Entry(question).Reference(q => q.Topic).LoadAsync(ct);

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
        var question = await context.Questions
            .FirstOrDefaultAsync(q => q.Id == id, ct);

        if (question == null)
            throw new InvalidOperationException($"Question {id} not found");

        question.TopicId = dto.TopicId;
        question.Difficulty = dto.Difficulty;
        question.QuestionText = dto.QuestionText;
        question.CorrectAnswer = dto.CorrectAnswer;
        question.SolutionSteps = dto.SolutionSteps;
        question.CommonMistakes = dto.CommonMistakes ?? new List<string>();

        await context.SaveChangesAsync(ct);

        var topicName = await context.Topics
            .Where(t => t.Id == question.TopicId)
            .Select(t => t.Name)
            .FirstOrDefaultAsync(ct) ?? string.Empty;

        return new AdminQuestionDto
        {
            Id = question.Id,
            TopicId = question.TopicId,
            TopicName = topicName,
            Difficulty = question.Difficulty,
            QuestionText = question.QuestionText,
            CorrectAnswer = question.CorrectAnswer
        };
    }

    public async Task DeleteQuestionAsync(int id, CancellationToken ct = default)
    {
        var question = await context.Questions.FindAsync(new object[] { id }, ct);
        if (question == null)
            throw new InvalidOperationException($"Question {id} not found");

        context.Questions.Remove(question);
        await context.SaveChangesAsync(ct);
    }

    // ========== TOPICS ==========
    public async Task<List<TopicDto>> GetAllTopicsAsync(CancellationToken ct = default)
    {
        return await context.Topics
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
        return await context.Students
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
        var totalStudents = await context.Students.CountAsync(ct);
        var totalQuestions = await context.Questions.CountAsync(ct);
        var totalAttempts = await context.Attempts.CountAsync(ct);

        var totalWorkItems = await context.WorkItems.CountAsync(ct);
        var completedWorkItems = await context.WorkItems
            .CountAsync(w => w.Status == Domain.Enums.WorkStatus.Done, ct);

        var successRate = totalWorkItems > 0 
            ? (double)completedWorkItems / totalWorkItems * 100 
            : 0;

        var processedItems = await context.WorkItems
            .Where(w => w.ProcessedAt != null)
            .Select(w => new { w.CreatedAt, ProcessedAt = w.ProcessedAt!.Value })
            .ToListAsync(ct);

        var avgProcessingTime = processedItems.Count > 0
            ? processedItems.Average(w => (w.ProcessedAt - w.CreatedAt).TotalMilliseconds)
            : 0;

        return new PerformanceMetricsDto
        {
            TotalStudents = totalStudents,
            TotalQuestions = totalQuestions,
            TotalAttempts = totalAttempts,
            TotalWorkItems = totalWorkItems,
            CompletedWorkItems = completedWorkItems,
            SuccessRate = successRate,
            AverageProcessingTimeMs = avgProcessingTime
        };
    }
    public async Task<StudentDto> CreateStudentAsync(string name, string email, CancellationToken ct = default)
    {
        var student = new Student
        {
            Name = name,
            Email = email,
            CreatedAt = DateTime.UtcNow
        };

        context.Students.Add(student);
        await context.SaveChangesAsync(ct);

        return new StudentDto
        {
            Id = student.Id,
            Name = student.Name,
            Email = student.Email,
            CreatedAt = student.CreatedAt
        };
    }
    
}
