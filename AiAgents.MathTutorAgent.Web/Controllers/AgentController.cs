using AiAgents.MathTutorAgent.Application.Services;
using AiAgents.MathTutorAgent.Domain.Entities;
using AiAgents.MathTutorAgent.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Text.Json;
using System.Security.Claims;

namespace AiAgents.MathTutorAgent.Web.Controllers;

[ApiController]
[Route("api/agent")]
[Authorize]
public class AgentController(
    WorkQueueService queueService,
    MathContentLocalizationService localizationService) : ControllerBase
{
    [HttpPost("next_question")]
    [EnableRateLimiting("agent-ops")]
    public async Task<IActionResult> NextQuestion([FromBody] NextQuestionRequest request, CancellationToken ct)
    {
        if (!CanAccessStudent(request.StudentId))
        {
            return Forbid();
        }

        var workItem = new WorkItem
        {
            StudentId = request.StudentId,
            Type = WorkItemType.NextQuestion,
            Status = WorkStatus.Queued,
            PayloadJson = JsonSerializer.Serialize(new
            {
                Language = request.Language
            }),
            CreatedAt = DateTime.UtcNow
        };

        var workItemId = await queueService.EnqueueAsync(workItem, ct);
        return Ok(new { WorkItemId = workItemId });
    }

    [HttpPost("submit_answer")]
    [EnableRateLimiting("agent-ops")]
    public async Task<IActionResult> SubmitAnswer([FromBody] SubmitAnswerRequest request, CancellationToken ct)
    {
        if (!CanAccessStudent(request.StudentId))
        {
            return Forbid();
        }

        var payload = new
        {
            QuestionId = request.QuestionId,
            Answer = request.Answer,
            TimeMs = request.TimeMs,
            TimedOut = request.TimedOut,
            Language = request.Language
        };

        var workItem = new WorkItem
        {
            StudentId = request.StudentId,
            Type = WorkItemType.SubmitAnswer,
            Status = WorkStatus.Queued,
            PayloadJson = JsonSerializer.Serialize(payload),
            CreatedAt = DateTime.UtcNow
        };

        var workItemId = await queueService.EnqueueAsync(workItem, ct);
        return Ok(new { WorkItemId = workItemId });
    }

    [HttpPost("explain")]
    [EnableRateLimiting("agent-ops")]
    public async Task<IActionResult> Explain([FromBody] ExplainRequest request, CancellationToken ct)
    {
        if (!CanAccessStudent(request.StudentId))
        {
            return Forbid();
        }

        var payload = new
        {
            QuestionId = request.QuestionId,
            TopicId = request.TopicId,
            ErrorTag = request.ErrorTag,
            Language = request.Language
        };

        var workItem = new WorkItem
        {
            StudentId = request.StudentId,
            Type = WorkItemType.Explain,
            Status = WorkStatus.Queued,
            PayloadJson = JsonSerializer.Serialize(payload),
            CreatedAt = DateTime.UtcNow
        };

        var workItemId = await queueService.EnqueueAsync(workItem, ct);
        return Ok(new { WorkItemId = workItemId });
    }

    [HttpPost("complete_milestone")]
    [EnableRateLimiting("agent-ops")]
    public async Task<IActionResult> CompleteMilestone(
        [FromBody] CompleteMilestoneRequest request,
        [FromServices] CrossMathMilestoneService milestoneService,
        CancellationToken ct)
    {
        if (!CanAccessStudent(request.StudentId))
        {
            return Forbid();
        }

        var nextMilestone = await milestoneService.CompleteMilestoneAsync(request.StudentId, request.ChallengeKey, ct);
        nextMilestone = localizationService.LocalizeMilestone(nextMilestone, request.Language);
        return Ok(new { NextMilestone = nextMilestone });
    }

    [HttpPost("upload_image")]
    [EnableRateLimiting("agent-ops")]
    public async Task<IActionResult> UploadImage([FromForm] IFormFile file, [FromForm] int studentId, CancellationToken ct)
    {
        if (!CanAccessStudent(studentId))
        {
            return Forbid();
        }

        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded");

        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream, ct);
        var imageBytes = memoryStream.ToArray();
        var base64Image = Convert.ToBase64String(imageBytes);

        var payload = new
        {
            ImageBase64 = base64Image,
            FileName = file.FileName
        };

        var workItem = new WorkItem
        {
            StudentId = studentId,
            Type = WorkItemType.UploadImage,
            Status = WorkStatus.Queued,
            PayloadJson = JsonSerializer.Serialize(payload),
            CreatedAt = DateTime.UtcNow
        };

        var workItemId = await queueService.EnqueueAsync(workItem, ct);
        return Ok(new { WorkItemId = workItemId });
    }

    private bool CanAccessStudent(int studentId)
    {
        if (User.IsInRole(UserRoles.Admin))
        {
            return true;
        }

        var claimValue = User.FindFirstValue("student_id");
        return int.TryParse(claimValue, out var currentStudentId) && currentStudentId == studentId;
    }
}

// Request DTOs
public record NextQuestionRequest(int StudentId, string? Language = null);
public record SubmitAnswerRequest(int StudentId, int QuestionId, string Answer, int TimeMs, bool TimedOut = false, string? Language = null);
public record ExplainRequest(int StudentId, int? QuestionId, int? TopicId, string? ErrorTag, string? Language = null);
public record CompleteMilestoneRequest(int StudentId, string ChallengeKey, string? Language = null);
