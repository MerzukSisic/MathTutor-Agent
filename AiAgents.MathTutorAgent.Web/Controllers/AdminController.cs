using AiAgents.MathTutorAgent.Application.DTOs;
using AiAgents.MathTutorAgent.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace AiAgents.MathTutorAgent.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdminController(AdminService adminService) : ControllerBase
{
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

    [HttpGet("topics")]
    public async Task<IActionResult> GetTopics(CancellationToken ct)
    {
        var topics = await adminService.GetAllTopicsAsync(ct);
        return Ok(topics);
    }

    [HttpGet("students")]
    public async Task<IActionResult> GetStudents(CancellationToken ct)
    {
        var students = await adminService.GetAllStudentsAsync(ct);
        return Ok(students);
    }

    [HttpGet("metrics")]
    public async Task<IActionResult> GetMetrics(CancellationToken ct)
    {
        var metrics = await adminService.GetPerformanceMetricsAsync(ct);
        return Ok(metrics);
    }
}