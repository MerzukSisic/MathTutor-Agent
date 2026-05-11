using AiAgents.MathTutorAgent.Application.DTOs;
using AiAgents.MathTutorAgent.Application.Services;
using AiAgents.MathTutorAgent.Domain.Entities;
using AiAgents.MathTutorAgent.ML.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiAgents.MathTutorAgent.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = UserRoles.Admin)]
public class AdminController(
    AdminService adminService,
    StudentProfileService studentProfileService,
    MlTrainingService mlTrainingService)
    : ControllerBase
{
    // ========== QUESTIONS CRUD ==========

    [HttpGet("questions")]
    public async Task<IActionResult> GetQuestions(CancellationToken ct)
    {
        var questions = await adminService.GetAllQuestionsAsync(ct);
        return Ok(questions);
    }

    [HttpPost("questions")]
    public async Task<IActionResult> CreateQuestion([FromBody] CreateQuestionDto dto, CancellationToken ct)
    {
        var question = await adminService.CreateQuestionAsync(dto, ct);
        return CreatedAtAction(nameof(GetQuestions), new { id = question.Id }, question);
    }

    [HttpPut("questions/{id}")]
    public async Task<IActionResult> UpdateQuestion(int id, [FromBody] CreateQuestionDto dto, CancellationToken ct)
    {
        var question = await adminService.UpdateQuestionAsync(id, dto, ct);
        return Ok(question);
    }

    [HttpDelete("questions/{id}")]
    public async Task<IActionResult> DeleteQuestion(int id, CancellationToken ct)
    {
        await adminService.DeleteQuestionAsync(id, ct);
        return NoContent();
    }

    // ========== TOPICS ==========

    [HttpGet("topics")]
    public async Task<IActionResult> GetTopics(CancellationToken ct)
    {
        var topics = await adminService.GetAllTopicsAsync(ct);
        return Ok(topics);
    }

    // ========== STUDENTS ==========

    [HttpGet("students")]
    public async Task<IActionResult> GetStudents(CancellationToken ct)
    {
        var students = await adminService.GetAllStudentsAsync(ct);
        return Ok(students);
    }

    // ========== METRICS ==========

    [HttpGet("performance-metrics")]
    public async Task<IActionResult> GetPerformanceMetrics(CancellationToken ct)
    {
        var metrics = await adminService.GetPerformanceMetricsAsync(ct);
        return Ok(metrics);
    }

    // ========== ML MODEL TRAINING (THIN - just call service) ==========

    [HttpPost("train-ml-models")]
    public async Task<IActionResult> TrainMlModels(CancellationToken ct)
    {
        // Web layer: receive request, call Application service, return response
        var result = await mlTrainingService.TrainAllModelsAsync(ct);

        if (!result.Success)
            return StatusCode(500, new { result.Success, Message = result.ErrorMessage });

        return Ok(new
        {
            result.Success,
            Message = "ML models trained successfully",
            Models = new
            {
                KnowledgeTracingModel = result.KnowledgeTracingModelPath,
                TopicClassifierModel = result.TopicClassifierModelPath
            },
            TrainingData = new
            {
                KnowledgeTracingSamples = result.KnowledgeTracingSamplesCount,
                TopicClassificationSamples = result.TopicClassificationSamplesCount,
                RealKnowledgeTracingSamples = result.RealKnowledgeTracingSamplesCount,
                RealTopicClassificationSamples = result.RealTopicClassificationSamplesCount
            }
        });
    }

    [HttpPost("reload-ml-models")]
    public async Task<IActionResult> ReloadMlModels(CancellationToken ct)
    {
        // Web layer: receive request, call Application service, return response
        var result = await mlTrainingService.ReloadModelsAsync(ct);

        if (!result.Success)
            return StatusCode(500, new { result.Success, Message = result.ErrorMessage });

        return Ok(result);
    }
    [HttpPost("students")]
    public async Task<IActionResult> CreateStudent([FromBody] CreateStudentRequest request, CancellationToken ct)
    {
        var student = await adminService.CreateStudentAsync(request.Name, request.Email, ct);
        return Ok(student);
    }
    [HttpGet("dashboard/stats")]
    public async Task<ActionResult<DashboardStatsDto>> GetDashboardStats(CancellationToken ct)
    {
        var stats = await studentProfileService.GetDashboardStatsAsync(ct);
        return Ok(stats);
    }

    public record CreateStudentRequest(string Name, string Email);
}
