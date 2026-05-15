using AiAgents.MathTutorAgent.Application.DTOs;
using AiAgents.MathTutorAgent.Domain.Entities;
using AiAgents.MathTutorAgent.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AiAgents.MathTutorAgent.Application.Services;

public class AdminService(MathTutorDbContext context) : IAdminService
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
            throw new KeyNotFoundException($"Question {id} not found.");

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
            throw new KeyNotFoundException($"Question {id} not found.");

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

    public async Task<StudentDto?> GetStudentByIdAsync(int id, CancellationToken ct = default)
    {
        return await context.Students
            .Where(s => s.Id == id)
            .Select(s => new StudentDto
            {
                Id = s.Id,
                Name = s.Name,
                Email = s.Email,
                CreatedAt = s.CreatedAt
            })
            .FirstOrDefaultAsync(ct);
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
    public async Task<StudentDto> CreateStudentAsync(CreateStudentDto dto, CancellationToken ct = default)
    {
        var name = dto.Name.Trim();
        var email = dto.Email.Trim();

        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Student name is required.");
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            throw new InvalidOperationException("Valid email is required.");

        var normalizedEmail = StringNormalizer.NormalizeEmail(email);
        if (await context.Students.AnyAsync(s => s.Email == normalizedEmail, ct))
            throw new InvalidOperationException("Student with this email already exists.");

        var student = new Student
        {
            Name = name,
            Email = normalizedEmail,
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

    public async Task<StudentDto> UpdateStudentAsync(int id, UpdateStudentDto dto, CancellationToken ct = default)
    {
        var student = await context.Students.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (student == null)
            throw new KeyNotFoundException($"Student {id} not found.");

        var name = dto.Name.Trim();
        var email = dto.Email.Trim();
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Student name is required.");
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            throw new InvalidOperationException("Valid email is required.");

        var normalizedEmail = StringNormalizer.NormalizeEmail(email);
        var emailTaken = await context.Students
            .AnyAsync(s => s.Id != id && s.Email == normalizedEmail, ct);
        if (emailTaken)
            throw new InvalidOperationException("Another student already uses this email.");

        student.Name = name;
        student.Email = normalizedEmail;

        var linkedAccounts = await context.UserAccounts
            .Where(a => a.StudentId == id)
            .ToListAsync(ct);

        var linkedAccountIds = linkedAccounts
            .Select(a => a.Id)
            .ToList();

        var accountEmailTaken = await context.UserAccounts.AnyAsync(
            a => a.Email == normalizedEmail && !linkedAccountIds.Contains(a.Id),
            ct);
        if (accountEmailTaken)
        {
            throw new InvalidOperationException("Email already belongs to another user account.");
        }

        foreach (var account in linkedAccounts)
        {
            account.FullName = name;
            account.Email = normalizedEmail;
        }

        await context.SaveChangesAsync(ct);

        return new StudentDto
        {
            Id = student.Id,
            Name = student.Name,
            Email = student.Email,
            CreatedAt = student.CreatedAt
        };
    }

    public async Task DeleteStudentAsync(int id, CancellationToken ct = default)
    {
        var student = await context.Students.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (student == null)
            throw new KeyNotFoundException($"Student {id} not found.");

        var hasProgressData = await context.Attempts.AnyAsync(a => a.StudentId == id, ct)
            || await context.StudentTopicStates.AnyAsync(s => s.StudentId == id, ct)
            || await context.RevisionScheduleItems.AnyAsync(r => r.StudentId == id, ct)
            || await context.WorkItems.AnyAsync(w => w.StudentId == id, ct);

        if (hasProgressData)
            throw new InvalidOperationException("Student has learning history and cannot be deleted. Edit student instead.");

        var linkedAccounts = await context.UserAccounts
            .Where(a => a.StudentId == id)
            .ToListAsync(ct);
        context.UserAccounts.RemoveRange(linkedAccounts);
        context.Students.Remove(student);
        await context.SaveChangesAsync(ct);
    }
}
