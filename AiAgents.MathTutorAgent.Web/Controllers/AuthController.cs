using System.Security.Claims;
using AiAgents.MathTutorAgent.Application.DTOs;
using AiAgents.MathTutorAgent.Application.Services;
using AiAgents.MathTutorAgent.Domain.Entities;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AiAgents.MathTutorAgent.Web.Controllers;

[ApiController]
[Route("api/auth")]
[EnableRateLimiting("auth")]
public class AuthController(
    IAuthService authService,
    IAntiforgery antiforgery,
    IConfiguration configuration) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request, CancellationToken ct)
    {
        var result = await authService.RegisterAsync(request, GetAppBaseUrl(), ct);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ValidateAntiForgeryToken]
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

    [AllowAnonymous]
    [HttpPost("confirm_email")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequestDto request, CancellationToken ct)
    {
        var result = await authService.ConfirmEmailAsync(request, ct);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [AllowAnonymous]
    [HttpPost("forgot_password")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto request, CancellationToken ct)
    {
        var result = await authService.RequestPasswordResetAsync(request, GetAppBaseUrl(), ct);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpPost("reset_password")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request, CancellationToken ct)
    {
        var result = await authService.ResetPasswordAsync(request, ct);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [AllowAnonymous]
    [HttpPost("resend_confirmation")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendConfirmation([FromBody] ForgotPasswordRequestDto request, CancellationToken ct)
    {
        var result = await authService.ResendEmailConfirmationAsync(request, GetAppBaseUrl(), ct);
        return Ok(result);
    }

    [Authorize]
    [HttpPost("logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    [AllowAnonymous]
    [HttpGet("csrf_token")]
    public IActionResult GetCsrfToken()
    {
        var tokens = antiforgery.GetAndStoreTokens(HttpContext);
        if (string.IsNullOrWhiteSpace(tokens.RequestToken))
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Unable to generate CSRF token." });
        }

        return Ok(new { token = tokens.RequestToken });
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
            EmailConfirmed = ParseBoolClaim("email_confirmed"),
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
            new(ClaimTypes.Role, user.Role),
            new("email_confirmed", user.EmailConfirmed.ToString().ToLowerInvariant())
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

    private bool ParseBoolClaim(string type)
    {
        var value = User.FindFirstValue(type);
        return bool.TryParse(value, out var parsed) && parsed;
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
