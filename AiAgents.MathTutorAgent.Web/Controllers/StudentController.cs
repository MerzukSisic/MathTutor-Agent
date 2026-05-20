using AiAgents.MathTutorAgent.Application.Services;
using AiAgents.MathTutorAgent.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AiAgents.MathTutorAgent.Web.Controllers;

[ApiController]
[Route("api/student")]
[Authorize]
public class StudentController(StudentProfileService profileService, PdfExportService pdfService)
    : ControllerBase
{
    [HttpGet("{studentId}/profile")]
    public async Task<IActionResult> GetProfile(int studentId, [FromQuery] string? lang = null)
    {
        if (!CanAccessStudent(studentId))
        {
            return Forbid();
        }

        try
        {
            var profile = await profileService.GetProfileAsync(studentId, lang, HttpContext.RequestAborted);
            return Ok(profile);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet("{studentId}/stats")]
    public async Task<IActionResult> GetStudyStats(int studentId, [FromQuery] GetStudyStatsQuery query)
    {
        if (!CanAccessStudent(studentId))
        {
            return Forbid();
        }

        var fromDate = query.From ?? DateTime.UtcNow.AddMonths(-1);
        var toDate = query.To ?? DateTime.UtcNow;

        // Ako klijent pošalje samo datum (00:00:00), tretiraj "to" kao kraj tog dana.
        if (query.To.HasValue && query.To.Value.TimeOfDay == TimeSpan.Zero)
        {
            toDate = query.To.Value.Date.AddDays(1).AddTicks(-1);
        }

        var stats = await profileService.GetStudySessionStatsAsync(studentId, fromDate, toDate, query.Lang, HttpContext.RequestAborted);
        return Ok(stats);
    }

    [HttpGet("{studentId}/export_pdf")]
    public async Task<IActionResult> ExportReport(int studentId, [FromQuery] string? lang = null)
    {
        if (!CanAccessStudent(studentId))
        {
            return Forbid();
        }

        try
        {
            var profile = await profileService.GetProfileAsync(studentId, lang, HttpContext.RequestAborted);
            var stats = await profileService.GetStudySessionStatsAsync(
                studentId,
                DateTime.UtcNow.AddMonths(-1),
                DateTime.UtcNow,
                lang,
                HttpContext.RequestAborted);

            var pdfBytes = pdfService.GenerateStudentReport(profile, stats, lang);

            return File(pdfBytes, "application/pdf", $"MathTutor_Report_{studentId}_{DateTime.Now:yyyyMMdd}.pdf");
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
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

    public sealed class GetStudyStatsQuery
    {
        public DateTime? From { get; init; }
        public DateTime? To { get; init; }
        public string? Lang { get; init; }
    }
}
