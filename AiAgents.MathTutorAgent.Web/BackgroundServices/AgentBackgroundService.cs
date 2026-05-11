using AiAgents.MathTutorAgent.Application.Runners;
using AiAgents.MathTutorAgent.Web.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace AiAgents.MathTutorAgent.Web.BackgroundServices;

public class AgentBackgroundService(
    IServiceProvider serviceProvider,
    IHubContext<AgentHub> hubContext,
    ILogger<AgentBackgroundService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("AgentBackgroundService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var runner = scope.ServiceProvider.GetRequiredService<MathTutoringAgentRunner>();

                // ✅ FIX: Use StepAsync (override method)
                var result = await runner.StepAsync(stoppingToken);

                if (result == null)
                {
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
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
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in agent tick");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        logger.LogInformation("AgentBackgroundService stopped");
    }
}
