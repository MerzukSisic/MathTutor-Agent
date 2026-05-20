using AiAgents.MathTutorAgent.Application.DTOs;
using AiAgents.MathTutorAgent.Application.Services;
using AiAgents.MathTutorAgent.Domain.Entities;
using AiAgents.MathTutorAgent.ML.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AiAgents.MathTutorAgent.Web.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = UserRoles.Admin)]
[AutoValidateAntiforgeryToken]
public class AdminController(
    IAdminService adminService,
    StudentProfileService studentProfileService,
    MlTrainingService mlTrainingService,
    MathContentLocalizationService localizationService,
    IConfiguration configuration)
    : ControllerBase
{
    // ========== QUESTIONS CRUD ==========

    [HttpGet("questions")]
    public async Task<IActionResult> GetQuestions([FromQuery] string? lang, CancellationToken ct)
    {
        var questions = await adminService.GetAllQuestionsAsync(ct);
        var language = localizationService.NormalizeLanguage(lang);
        var localized = questions
            .Select(question => question with
            {
                TopicName = localizationService.LocalizeTopicName(question.TopicName, language),
                QuestionText = localizationService.LocalizeQuestionText(question.QuestionText, language),
                CorrectAnswer = localizationService.LocalizeAnswerToken(question.CorrectAnswer, language)
            })
            .ToList();

        return Ok(localized);
    }

    [HttpPost("questions")]
    [EnableRateLimiting("admin-write")]
    public async Task<IActionResult> CreateQuestion([FromBody] CreateQuestionDto dto, CancellationToken ct)
    {
        var question = await adminService.CreateQuestionAsync(dto, ct);
        return CreatedAtAction(nameof(GetQuestions), new { id = question.Id }, question);
    }

    [HttpPut("questions/{id}")]
    [EnableRateLimiting("admin-write")]
    public async Task<IActionResult> UpdateQuestion(int id, [FromBody] CreateQuestionDto dto, CancellationToken ct)
    {
        try
        {
            var question = await adminService.UpdateQuestionAsync(id, dto, ct);
            return Ok(question);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
    }

    [HttpDelete("questions/{id}")]
    [EnableRateLimiting("admin-write")]
    public async Task<IActionResult> DeleteQuestion(int id, CancellationToken ct)
    {
        try
        {
            await adminService.DeleteQuestionAsync(id, ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
    }

    // ========== TOPICS ==========

    [HttpGet("topics")]
    public async Task<IActionResult> GetTopics([FromQuery] string? lang, CancellationToken ct)
    {
        var topics = await adminService.GetAllTopicsAsync(ct);
        var language = localizationService.NormalizeLanguage(lang);
        var localized = topics
            .Select(topic => topic with
            {
                Name = localizationService.LocalizeTopicName(topic.Name, language),
                Area = localizationService.LocalizeAreaName(topic.Area, language)
            })
            .ToList();

        return Ok(localized);
    }

    // ========== STUDENTS ==========

    [HttpGet("students")]
    public async Task<IActionResult> GetStudents(CancellationToken ct)
    {
        var students = await adminService.GetAllStudentsAsync(ct);
        return Ok(students);
    }

    [HttpGet("students/{id:int}")]
    public async Task<IActionResult> GetStudentById(int id, CancellationToken ct)
    {
        var student = await adminService.GetStudentByIdAsync(id, ct);
        if (student == null)
        {
            return NotFound(new { Message = $"Student {id} not found." });
        }

        return Ok(student);
    }

    // ========== METRICS ==========

    [HttpGet("performance_metrics")]
    public async Task<IActionResult> GetPerformanceMetrics(CancellationToken ct)
    {
        var metrics = await adminService.GetPerformanceMetricsAsync(ct);
        return Ok(metrics);
    }

    // ========== ML MODEL TRAINING (THIN - just call service) ==========

    [HttpPost("train_ml_models")]
    [EnableRateLimiting("admin-write")]
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

    [HttpPost("reload_ml_models")]
    [EnableRateLimiting("admin-write")]
    public async Task<IActionResult> ReloadMlModels(CancellationToken ct)
    {
        // Web layer: receive request, call Application service, return response
        var result = await mlTrainingService.ReloadModelsAsync(ct);

        if (!result.Success)
            return StatusCode(500, new { result.Success, Message = result.ErrorMessage });

        return Ok(result);
    }
    [HttpPost("students")]
    [EnableRateLimiting("admin-write")]
    public async Task<IActionResult> CreateStudent([FromBody] CreateStudentDto request, CancellationToken ct)
    {
        try
        {
            var result = await adminService.CreateStudentAsync(request, GetAppBaseUrl(), ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpPut("students/{id:int}")]
    [EnableRateLimiting("admin-write")]
    public async Task<IActionResult> UpdateStudent(int id, [FromBody] UpdateStudentDto request, CancellationToken ct)
    {
        try
        {
            var student = await adminService.UpdateStudentAsync(id, request, ct);
            return Ok(student);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpDelete("students/{id:int}")]
    [EnableRateLimiting("admin-write")]
    public async Task<IActionResult> DeleteStudent(int id, CancellationToken ct)
    {
        try
        {
            await adminService.DeleteStudentAsync(id, ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }
    [HttpGet("dashboard/stats")]
    public async Task<ActionResult<DashboardStatsDto>> GetDashboardStats(CancellationToken ct)
    {
        var stats = await studentProfileService.GetDashboardStatsAsync(ct);
        return Ok(stats);
    }

    private string GetAppBaseUrl()
    {
        var configuredBaseUrl = configuration["App:PublicBaseUrl"];
        if (!string.IsNullOrWhiteSpace(configuredBaseUrl))
        {
            return configuredBaseUrl.Trim().TrimEnd('/');
        }

        return "http://localhost:5297";
    }
}
