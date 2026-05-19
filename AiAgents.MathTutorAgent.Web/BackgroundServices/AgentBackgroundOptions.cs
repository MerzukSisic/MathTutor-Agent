namespace AiAgents.MathTutorAgent.Web.BackgroundServices;

public sealed class AgentBackgroundOptions
{
    public int IdleDelaySeconds { get; set; } = 2;
    public int ErrorDelaySeconds { get; set; } = 5;
}
