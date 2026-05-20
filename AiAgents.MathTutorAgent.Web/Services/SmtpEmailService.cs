using System.Net;
using System.Net.Mail;
using AiAgents.MathTutorAgent.Application.Services;
using Microsoft.Extensions.Options;

namespace AiAgents.MathTutorAgent.Web.Services;

public class SmtpEmailService(
    IOptions<EmailSettings> settings,
    ILogger<SmtpEmailService> logger)
    : IEmailService
{
    private static readonly Action<ILogger, string, string, Exception?> LogEmailDisabledMessage =
        LoggerMessage.Define<string, string>(
            LogLevel.Information,
            new EventId(2100, nameof(LogEmailDisabledMessage)),
            "Email disabled. Skipping message to {To}. Subject: {Subject}");

    private static readonly Action<ILogger, string, Exception?> LogEmailSentMessage =
        LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(2101, nameof(LogEmailSentMessage)),
            "Email sent to {To}");

    private readonly EmailSettings emailSettings = settings.Value;
    public bool IsEnabled => emailSettings.Enabled;

    public async Task SendEmailAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        if (!emailSettings.Enabled)
        {
            LogEmailDisabledMessage(logger, to, subject, null);
            return;
        }

        using var message = new MailMessage
        {
            From = new MailAddress(emailSettings.FromEmail, emailSettings.FromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        message.To.Add(to);

        using var client = new SmtpClient(emailSettings.SmtpHost, emailSettings.SmtpPort)
        {
            EnableSsl = emailSettings.UseSsl,
            Credentials = new NetworkCredential(emailSettings.SmtpUsername, emailSettings.SmtpPassword)
        };

        await client.SendMailAsync(message, ct);
        LogEmailSentMessage(logger, to, null);
    }
}
