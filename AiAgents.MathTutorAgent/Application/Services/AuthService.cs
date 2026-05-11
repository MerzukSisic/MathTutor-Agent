using AiAgents.MathTutorAgent.Application.DTOs;
using AiAgents.MathTutorAgent.Domain.Entities;
using AiAgents.MathTutorAgent.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AiAgents.MathTutorAgent.Application.Services;

public class AuthService(
    MathTutorDbContext context,
    PasswordHashingService passwordHashingService,
    IEmailService emailService)
{
    public async Task<AuthResultDto> RegisterAsync(RegisterRequestDto request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            return new AuthResultDto { Success = false, Message = "Full name is required." };
        }

        if (string.IsNullOrWhiteSpace(request.Email) || !IsValidEmail(request.Email))
        {
            return new AuthResultDto { Success = false, Message = "Valid email is required." };
        }

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
        {
            return new AuthResultDto { Success = false, Message = "Password must be at least 6 characters." };
        }

        var email = NormalizeEmail(request.Email);
        if (await context.UserAccounts.AnyAsync(u => u.Email == email, ct))
        {
            return new AuthResultDto { Success = false, Message = "Email already in use." };
        }

        Student? student = await context.Students.FirstOrDefaultAsync(s => s.Email == email, ct);
        if (student == null)
        {
            student = new Student
            {
                Name = request.FullName.Trim(),
                Email = email,
                CreatedAt = DateTime.UtcNow
            };
            context.Students.Add(student);
            await context.SaveChangesAsync(ct);
        }

        var account = new UserAccount
        {
            FullName = request.FullName.Trim(),
            Email = email,
            PasswordHash = passwordHashingService.HashPassword(request.Password),
            Role = UserRoles.Student,
            EmailConfirmed = false,
            StudentId = student.Id,
            CreatedAt = DateTime.UtcNow
        };

        context.UserAccounts.Add(account);
        await context.SaveChangesAsync(ct);

        _ = SendWelcomeEmailSafeAsync(account, ct);

        return new AuthResultDto
        {
            Success = true,
            Message = "Account created successfully.",
            User = ToDto(account)
        };
    }

    public async Task<AuthResultDto> LoginAsync(LoginRequestDto request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return new AuthResultDto { Success = false, Message = "Email and password are required." };
        }

        var email = NormalizeEmail(request.Email);
        var account = await context.UserAccounts.FirstOrDefaultAsync(u => u.Email == email, ct);
        if (account == null || !passwordHashingService.VerifyPassword(request.Password, account.PasswordHash))
        {
            return new AuthResultDto { Success = false, Message = "Invalid email or password." };
        }

        return new AuthResultDto
        {
            Success = true,
            Message = "Login successful.",
            User = ToDto(account)
        };
    }

    private async Task SendWelcomeEmailSafeAsync(UserAccount account, CancellationToken ct)
    {
        try
        {
            var subject = "Welcome to MathTutor AI";
            var body = $"<h2>Welcome, {account.FullName}!</h2><p>Your account is ready. You can now sign in and start learning.</p>";
            await emailService.SendEmailAsync(account.Email, subject, body, ct);
        }
        catch
        {
            // Email should not block registration flow.
        }
    }

    public static string NormalizeEmail(string email)
        => email.Trim().ToLowerInvariant();

    private static bool IsValidEmail(string email)
    {
        var value = email.Trim();
        var at = value.IndexOf('@');
        return at > 0 && at < value.Length - 1;
    }

    public static AuthUserDto ToDto(UserAccount account)
    {
        return new AuthUserDto
        {
            Id = account.Id,
            FullName = account.FullName,
            Email = account.Email,
            Role = account.Role,
            StudentId = account.StudentId
        };
    }
}
