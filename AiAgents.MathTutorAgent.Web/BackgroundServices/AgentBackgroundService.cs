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
    private readonly AgentBackgroundOptions _options = optionsAccessor.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("AgentBackgroundService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var runner = scope.ServiceProvider.GetRequiredService<MathTutoringAgentRunner>();

                var result = await runner.StepAsync(stoppingToken);

                if (result == null)
                {
                    await Task.Delay(TimeSpan.FromSeconds(_options.IdleDelaySeconds), stoppingToken);
                }
                else
                {
                    await hubContext.Clients.Group(AgentHub.GetStudentGroup(result.StudentId)).SendAsync(
                        "AgentTick",
                        result,
                        stoppingToken);

                    logger.LogInformation("Processed work item {WorkItemId}: {Outcome}",
                        result.WorkItemId, result.Outcome);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in agent tick");
                await Task.Delay(TimeSpan.FromSeconds(_options.ErrorDelaySeconds), stoppingToken);
            }
        }

        logger.LogInformation("AgentBackgroundService stopped");
    }
}
