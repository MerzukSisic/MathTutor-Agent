namespace AiAgents.MathTutorAgent.Application.DTOs;

public record RegisterRequestDto(string FullName, string Email, string Password);
public record LoginRequestDto(string Email, string Password);
public record ConfirmEmailRequestDto(string Email, string Token);
public record ForgotPasswordRequestDto(string Email);
public record ResetPasswordRequestDto(string Email, string Token, string NewPassword);

public class AuthUserDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool EmailConfirmed { get; set; }
    public int? StudentId { get; set; }
}

public class AuthResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public AuthUserDto? User { get; set; }
}

public class AuthStatusDto
{
    public bool IsAuthenticated { get; set; }
    public AuthUserDto? User { get; set; }
}
