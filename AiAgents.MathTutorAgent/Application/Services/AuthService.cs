using AiAgents.MathTutorAgent.Application.DTOs;
using System.Security.Cryptography;
using System.Text;
using AiAgents.MathTutorAgent.Domain.Entities;
using AiAgents.MathTutorAgent.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AiAgents.MathTutorAgent.Application.Services;

public class AuthService(
    MathTutorDbContext context,
    PasswordHashingService passwordHashingService,
    IEmailService emailService)
    : IAuthService
{
    public async Task<AuthResultDto> RegisterAsync(RegisterRequestDto request, string appBaseUrl, CancellationToken ct = default)
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

        var email = StringNormalizer.NormalizeEmail(request.Email);
        if (await context.UserAccounts.AnyAsync(u => u.Email == email, ct))
        {
            return new AuthResultDto
            {
                Success = false,
                Message = "Email already in use. If your teacher invited you, use Forgot Password to set your password."
            };
        }

        Student? student = await context.Students
            .FirstOrDefaultAsync(s => s.Email == email, ct);
        if (student == null)
        {
            student = new Student
            {
                Name = request.FullName.Trim(),
                Email = email,
                CreatedAt = DateTime.UtcNow
            };
            context.Students.Add(student);
        }

        var account = new UserAccount
        {
            FullName = request.FullName.Trim(),
            Email = email,
            PasswordHash = passwordHashingService.HashPassword(request.Password),
            Role = UserRoles.Student,
            EmailConfirmed = true,
            Student = student,
            CreatedAt = DateTime.UtcNow
        };

        context.UserAccounts.Add(account);
        await context.SaveChangesAsync(ct);

        if (emailService.IsEnabled)
        {
            _ = SendRegistrationEmailsSafeAsync(account, appBaseUrl, ct);
        }

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

        var email = StringNormalizer.NormalizeEmail(request.Email);
        var account = await context.UserAccounts.FirstOrDefaultAsync(u => u.Email == email, ct);
        if (account == null || !passwordHashingService.VerifyPassword(request.Password, account.PasswordHash))
        {
            return new AuthResultDto { Success = false, Message = "Invalid email or password." };
        }

        if (!account.EmailConfirmed)
        {
            return new AuthResultDto
            {
                Success = false,
                Message = "Please confirm your email before logging in."
            };
        }

        return new AuthResultDto
        {
            Success = true,
            Message = "Login successful.",
            User = ToDto(account)
        };
    }

    public async Task<AuthResultDto> ConfirmEmailAsync(ConfirmEmailRequestDto request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Token))
        {
            return new AuthResultDto { Success = false, Message = "Email and token are required." };
        }

        var normalizedEmail = StringNormalizer.NormalizeEmail(request.Email);
        var tokenHash = HashToken(request.Token);

        var token = await context.AuthTokens
            .Include(t => t.UserAccount)
            .FirstOrDefaultAsync(t =>
                t.Purpose == AuthTokenPurposes.EmailConfirmation &&
                t.TokenHash == tokenHash, ct);

        if (token == null || !string.Equals(token.UserAccount.Email, normalizedEmail, StringComparison.Ordinal))
        {
            return new AuthResultDto { Success = false, Message = "Invalid or expired confirmation link." };
        }

        var now = DateTime.UtcNow;
        if (token.ConsumedAtUtc.HasValue || token.ExpiresAtUtc <= now)
        {
            return new AuthResultDto { Success = false, Message = "This confirmation link is no longer valid." };
        }

        token.UserAccount.EmailConfirmed = true;
        token.ConsumedAtUtc = now;
        await InvalidateActiveTokensAsync(token.UserAccountId, AuthTokenPurposes.EmailConfirmation, now, ct);
        await context.SaveChangesAsync(ct);

        return new AuthResultDto
        {
            Success = true,
            Message = "Email confirmed successfully. You can now log in.",
            User = ToDto(token.UserAccount)
        };
    }

    public async Task<AuthResultDto> RequestPasswordResetAsync(ForgotPasswordRequestDto request, string appBaseUrl, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || !IsValidEmail(request.Email))
        {
            return new AuthResultDto { Success = true, Message = "If the account exists, a reset link has been sent." };
        }

        var email = StringNormalizer.NormalizeEmail(request.Email);
        var account = await context.UserAccounts.FirstOrDefaultAsync(u => u.Email == email, ct);
        if (account == null || !account.EmailConfirmed)
        {
            return new AuthResultDto { Success = true, Message = "If the account exists, a reset link has been sent." };
        }

        _ = SendPasswordResetEmailSafeAsync(account, appBaseUrl, ct);
        return new AuthResultDto { Success = true, Message = "If the account exists, a reset link has been sent." };
    }

    public async Task<AuthResultDto> ResetPasswordAsync(ResetPasswordRequestDto request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Token) ||
            string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return new AuthResultDto { Success = false, Message = "Email, token, and new password are required." };
        }

        if (request.NewPassword.Length < 6)
        {
            return new AuthResultDto { Success = false, Message = "Password must be at least 6 characters." };
        }

        var normalizedEmail = StringNormalizer.NormalizeEmail(request.Email);
        var tokenHash = HashToken(request.Token);
        var token = await context.AuthTokens
            .Include(t => t.UserAccount)
            .FirstOrDefaultAsync(t =>
                t.Purpose == AuthTokenPurposes.PasswordReset &&
                t.TokenHash == tokenHash, ct);

        if (token == null || !string.Equals(token.UserAccount.Email, normalizedEmail, StringComparison.Ordinal))
        {
            return new AuthResultDto { Success = false, Message = "Invalid or expired reset link." };
        }

        var now = DateTime.UtcNow;
        if (token.ConsumedAtUtc.HasValue || token.ExpiresAtUtc <= now)
        {
            return new AuthResultDto { Success = false, Message = "This reset link is no longer valid." };
        }

        token.UserAccount.PasswordHash = passwordHashingService.HashPassword(request.NewPassword);
        token.ConsumedAtUtc = now;
        await InvalidateActiveTokensAsync(token.UserAccountId, AuthTokenPurposes.PasswordReset, now, ct);
        await context.SaveChangesAsync(ct);

        return new AuthResultDto { Success = true, Message = "Password has been reset. You can now log in." };
    }

    public async Task<AuthResultDto> ResendEmailConfirmationAsync(ForgotPasswordRequestDto request, string appBaseUrl, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || !IsValidEmail(request.Email))
        {
            return new AuthResultDto { Success = true, Message = "If the account exists, a confirmation email has been sent." };
        }

        var email = StringNormalizer.NormalizeEmail(request.Email);
        var account = await context.UserAccounts.FirstOrDefaultAsync(u => u.Email == email, ct);
        if (account == null || account.EmailConfirmed)
        {
            return new AuthResultDto { Success = true, Message = "If the account exists, a confirmation email has been sent." };
        }

        _ = SendEmailConfirmationSafeAsync(account, appBaseUrl, ct);
        return new AuthResultDto { Success = true, Message = "If the account exists, a confirmation email has been sent." };
    }

    private async Task SendRegistrationEmailsSafeAsync(UserAccount account, string appBaseUrl, CancellationToken ct)
    {
        try
        {
            await SendEmailConfirmationInternalAsync(account, appBaseUrl, ct);
        }
        catch
        {
            // Email should not block registration flow.
        }
    }

    private async Task SendEmailConfirmationSafeAsync(UserAccount account, string appBaseUrl, CancellationToken ct)
    {
        try
        {
            await SendEmailConfirmationInternalAsync(account, appBaseUrl, ct);
        }
        catch
        {
            // Email resend should not fail user flow.
        }
    }

    private async Task SendPasswordResetEmailSafeAsync(UserAccount account, string appBaseUrl, CancellationToken ct)
    {
        try
        {
            var token = await CreateTokenAsync(account.Id, AuthTokenPurposes.PasswordReset, TimeSpan.FromHours(1), ct);
            var resetLink = BuildPublicLink(appBaseUrl, "reset_password",
                ("email", account.Email),
                ("token", token));
            var subject = "Reset your MathTutor AI password";
            var body = $"""
                        <h2>Password reset request</h2>
                        <p>Hello {account.FullName},</p>
                        <p>Click the link below to reset your password:</p>
                        <p><a href="{resetLink}">{resetLink}</a></p>
                        <p>This link expires in 1 hour.</p>
                        """;
            await emailService.SendEmailAsync(account.Email, subject, body, ct);
        }
        catch
        {
            // Password reset email should not expose failures.
        }
    }

    private async Task SendEmailConfirmationInternalAsync(UserAccount account, string appBaseUrl, CancellationToken ct)
    {
        var token = await CreateTokenAsync(account.Id, AuthTokenPurposes.EmailConfirmation, TimeSpan.FromHours(24), ct);
        var confirmationLink = BuildPublicLink(appBaseUrl, "confirm_email",
            ("email", account.Email),
            ("token", token));
        var subject = "Confirm your MathTutor AI account";
        var body = $"""
                    <h2>Welcome to MathTutor AI</h2>
                    <p>Hello {account.FullName},</p>
                    <p>Please confirm your email address by clicking the link below:</p>
                    <p><a href="{confirmationLink}">{confirmationLink}</a></p>
                    <p>This link expires in 24 hours.</p>
                    """;
        await emailService.SendEmailAsync(account.Email, subject, body, ct);
    }

    private async Task<string> CreateTokenAsync(int userAccountId, string purpose, TimeSpan lifetime, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        await InvalidateActiveTokensAsync(userAccountId, purpose, now, ct);

        var token = GenerateToken();
        context.AuthTokens.Add(new AuthToken
        {
            UserAccountId = userAccountId,
            Purpose = purpose,
            TokenHash = HashToken(token),
            CreatedAtUtc = now,
            ExpiresAtUtc = now.Add(lifetime)
        });
        await context.SaveChangesAsync(ct);
        return token;
    }

    private async Task InvalidateActiveTokensAsync(int userAccountId, string purpose, DateTime utcNow, CancellationToken ct)
    {
        var activeTokens = await context.AuthTokens
            .Where(t => t.UserAccountId == userAccountId && t.Purpose == purpose && t.ConsumedAtUtc == null && t.ExpiresAtUtc > utcNow)
            .ToListAsync(ct);

        foreach (var item in activeTokens)
        {
            item.ConsumedAtUtc = utcNow;
        }
    }

    private static string BuildPublicLink(string appBaseUrl, string path, params (string Key, string Value)[] queryParts)
    {
        var baseUrl = string.IsNullOrWhiteSpace(appBaseUrl)
            ? "http://localhost:5297"
            : appBaseUrl.TrimEnd('/');

        var query = string.Join("&",
            queryParts.Select(x => $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value)}"));

        return $"{baseUrl}/{path}?{query}";
    }

    private static string GenerateToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }

    private static string HashToken(string token)
    {
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }

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
            EmailConfirmed = account.EmailConfirmed,
            StudentId = account.StudentId
        };
    }
}
