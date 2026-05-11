using System.Security.Claims;
using AiAgents.MathTutorAgent.Application.DTOs;
using AiAgents.MathTutorAgent.Application.Services;
using AiAgents.MathTutorAgent.Domain.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiAgents.MathTutorAgent.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(AuthService authService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request, CancellationToken ct)
    {
        var result = await authService.RegisterAsync(request, ct);
        if (!result.Success || result.User == null)
        {
            return BadRequest(result);
        }

        await SignInAsync(result.User);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request, CancellationToken ct)
    {
        var result = await authService.LoginAsync(request, ct);
        if (!result.Success || result.User == null)
        {
            return Unauthorized(result);
        }

        await SignInAsync(result.User);
        return Ok(result);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok(new { Success = true });
    }

    [AllowAnonymous]
    [HttpGet("me")]
    public IActionResult Me()
    {
        if (User?.Identity?.IsAuthenticated != true)
        {
            return Ok(new AuthStatusDto { IsAuthenticated = false });
        }

        var user = new AuthUserDto
        {
            Id = ParseIntClaim(ClaimTypes.NameIdentifier),
            FullName = User.FindFirstValue(ClaimTypes.Name) ?? string.Empty,
            Email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
            Role = User.FindFirstValue(ClaimTypes.Role) ?? UserRoles.Student,
            StudentId = ParseNullableIntClaim("student_id")
        };

        return Ok(new AuthStatusDto
        {
            IsAuthenticated = true,
            User = user
        });
    }

    private async Task SignInAsync(AuthUserDto user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role)
        };

        if (user.StudentId.HasValue)
        {
            claims.Add(new Claim("student_id", user.StudentId.Value.ToString()));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(14)
            });
    }

    private int ParseIntClaim(string type)
    {
        var value = User.FindFirstValue(type);
        return int.TryParse(value, out var parsed) ? parsed : 0;
    }

    private int? ParseNullableIntClaim(string type)
    {
        var value = User.FindFirstValue(type);
        return int.TryParse(value, out var parsed) ? parsed : null;
    }
}
