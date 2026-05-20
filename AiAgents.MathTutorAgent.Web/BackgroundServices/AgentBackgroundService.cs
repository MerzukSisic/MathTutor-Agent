using AiAgents.MathTutorAgent.Application.Runners;
using AiAgents.MathTutorAgent.Web.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace AiAgents.MathTutorAgent.Web.BackgroundServices;

public class AgentBackgroundService(
    IServiceProvider serviceProvider,
    IHubContext<AgentHub> hubContext,
    IOptions<AgentBackgroundOptions> optionsAccessor,
    ILogger<AgentBackgroundService> logger)
    : BackgroundService
{
    private static readonly Action<ILogger, Exception?> LogServiceStartedMessage =
        LoggerMessage.Define(
            LogLevel.Information,
            new EventId(2000, nameof(LogServiceStartedMessage)),
            "AgentBackgroundService started");

    private static readonly Action<ILogger, int, object?, Exception?> LogWorkItemProcessedMessage =
        LoggerMessage.Define<int, object?>(
            LogLevel.Information,
            new EventId(2001, nameof(LogWorkItemProcessedMessage)),
            "Processed work item {WorkItemId}: {Outcome}");

    private static readonly Action<ILogger, Exception?> LogTickErrorMessage =
        LoggerMessage.Define(
            LogLevel.Error,
            new EventId(2002, nameof(LogTickErrorMessage)),
            "Error in agent tick");

    private static readonly Action<ILogger, Exception?> LogServiceStoppedMessage =
        LoggerMessage.Define(
            LogLevel.Information,
            new EventId(2003, nameof(LogServiceStoppedMessage)),
            "AgentBackgroundService stopped");

    private readonly AgentBackgroundOptions _options = optionsAccessor.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogServiceStartedMessage(logger, null);
        var minIdleDelaySeconds = Math.Max(1, _options.IdleDelaySeconds);
        var maxIdleDelaySeconds = Math.Max(minIdleDelaySeconds, _options.MaxIdleDelaySeconds);
        var currentIdleDelaySeconds = minIdleDelaySeconds;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var runner = scope.ServiceProvider.GetRequiredService<MathTutoringAgentRunner>();

                var result = await runner.StepAsync(stoppingToken);

                if (result == null)
                {
                    await Task.Delay(TimeSpan.FromSeconds(currentIdleDelaySeconds), stoppingToken);
                    currentIdleDelaySeconds = Math.Min(maxIdleDelaySeconds, currentIdleDelaySeconds * 2);
                }
                else
                {
                    currentIdleDelaySeconds = minIdleDelaySeconds;
                    await hubContext.Clients.Group(AgentHub.GetStudentGroup(result.StudentId)).SendAsync(
                        "AgentTick",
                        result,
                        stoppingToken);

                    LogWorkItemProcessedMessage(logger, result.WorkItemId, result.Outcome, null);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                LogTickErrorMessage(logger, ex);
                currentIdleDelaySeconds = minIdleDelaySeconds;
                await Task.Delay(TimeSpan.FromSeconds(_options.ErrorDelaySeconds), stoppingToken);
            }
        }

        LogServiceStoppedMessage(logger, null);
    }
}
