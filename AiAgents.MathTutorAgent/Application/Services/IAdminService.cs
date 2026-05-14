using AiAgents.MathTutorAgent.Application.DTOs;

namespace AiAgents.MathTutorAgent.Application.Services;

public interface IAdminService
{
    Task<List<AdminQuestionDto>> GetAllQuestionsAsync(CancellationToken ct = default);
    Task<AdminQuestionDto> CreateQuestionAsync(CreateQuestionDto dto, CancellationToken ct = default);
    Task<AdminQuestionDto> UpdateQuestionAsync(int id, CreateQuestionDto dto, CancellationToken ct = default);
    Task DeleteQuestionAsync(int id, CancellationToken ct = default);
    Task<List<TopicDto>> GetAllTopicsAsync(CancellationToken ct = default);
    Task<List<StudentDto>> GetAllStudentsAsync(CancellationToken ct = default);
    Task<StudentDto?> GetStudentByIdAsync(int id, CancellationToken ct = default);
    Task<PerformanceMetricsDto> GetPerformanceMetricsAsync(CancellationToken ct = default);
    Task<StudentDto> CreateStudentAsync(CreateStudentDto dto, CancellationToken ct = default);
    Task<StudentDto> UpdateStudentAsync(int id, UpdateStudentDto dto, CancellationToken ct = default);
    Task DeleteStudentAsync(int id, CancellationToken ct = default);
}
