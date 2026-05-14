namespace AiAgents.MathTutorAgent.Application.Services;

public interface IEmailService
{
    bool IsEnabled { get; }
    Task SendEmailAsync(string to, string subject, string htmlBody, CancellationToken ct = default);
}
