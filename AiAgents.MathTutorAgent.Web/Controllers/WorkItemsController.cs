using AiAgents.MathTutorAgent.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace AiAgents.MathTutorAgent.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WorkItemsController(WorkQueueService queueService) : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<IActionResult> GetWorkItem(int id, CancellationToken ct)
    {
        var workItem = await queueService.GetWorkItemAsync(id, ct);
        if (workItem == null)
            return NotFound();

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

        return Ok(new { Status = workItem.Status.ToString() });
    }
}