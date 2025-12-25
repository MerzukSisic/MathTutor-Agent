using AiAgents.MathTutorAgent.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace AiAgents.MathTutorAgent.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StudentController(StudentProfileService profileService, PdfExportService pdfService)
    : ControllerBase
{
    [HttpGet("{studentId}/profile")]
    public async Task<IActionResult> GetProfile(int studentId)
    {
        try
        {
            var profile = await profileService.GetProfileAsync(studentId, HttpContext.RequestAborted);
            return Ok(profile);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet("{studentId}/stats")]
    public async Task<IActionResult> GetStudyStats(int studentId, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var fromDate = from ?? DateTime.UtcNow.AddMonths(-1);
        var toDate = to ?? DateTime.UtcNow;

        var stats = await profileService.GetStudySessionStatsAsync(studentId, fromDate, toDate, HttpContext.RequestAborted);
        return Ok(stats);
    }

    [HttpGet("{studentId}/export-pdf")]
    public async Task<IActionResult> ExportReport(int studentId)
    {
        try
        {
            var profile = await profileService.GetProfileAsync(studentId, HttpContext.RequestAborted);
            var stats = await profileService.GetStudySessionStatsAsync(
                studentId, 
                DateTime.UtcNow.AddMonths(-1), 
                DateTime.UtcNow, 
                HttpContext.RequestAborted);

            var pdfBytes = pdfService.GenerateStudentReport(profile, stats);

            return File(pdfBytes, "application/pdf", $"MathTutor_Report_{studentId}_{DateTime.Now:yyyyMMdd}.pdf");
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}