using AiAgents.MathTutorAgent.Application.Services;
using AiAgents.MathTutorAgent.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AiAgents.MathTutorAgent.Web.Controllers;

[ApiController]
[Route("api/work_items")]
[Authorize]
public class WorkItemsController(WorkQueueService queueService) : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<IActionResult> GetWorkItem(int id, CancellationToken ct)
    {
        var workItem = await queueService.GetWorkItemAsync(id, ct);
        if (workItem == null)
            return NotFound();

        if (!CanAccessStudent(workItem.StudentId))
        {
            return Forbid();
        }

        return Ok(new
        {
            workItem.Id,
            workItem.Status,
            workItem.Type,
            workItem.CreatedAt,
            workItem.ProcessedAt,
            workItem.ResultJson
        });
    }

    [HttpGet("{id}/status")]
    public async Task<IActionResult> GetStatus(int id, CancellationToken ct)
    {
        var workItem = await queueService.GetWorkItemAsync(id, ct);
        if (workItem == null)
            return NotFound();

        if (!CanAccessStudent(workItem.StudentId))
        {
            return Forbid();
        }

        return Ok(new { Status = workItem.Status.ToString() });
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
