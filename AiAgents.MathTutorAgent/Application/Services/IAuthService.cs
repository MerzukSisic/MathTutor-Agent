using AiAgents.MathTutorAgent.Application.DTOs;

namespace AiAgents.MathTutorAgent.Application.Services;

public interface IAuthService
{
    Task<AuthResultDto> RegisterAsync(RegisterRequestDto request, string appBaseUrl, CancellationToken ct = default);
    Task<AuthResultDto> LoginAsync(LoginRequestDto request, CancellationToken ct = default);
    Task<AuthResultDto> ConfirmEmailAsync(ConfirmEmailRequestDto request, CancellationToken ct = default);
    Task<AuthResultDto> RequestPasswordResetAsync(ForgotPasswordRequestDto request, string appBaseUrl, CancellationToken ct = default);
    Task<AuthResultDto> ResetPasswordAsync(ResetPasswordRequestDto request, CancellationToken ct = default);
    Task<AuthResultDto> ResendEmailConfirmationAsync(ForgotPasswordRequestDto request, string appBaseUrl, CancellationToken ct = default);
}
