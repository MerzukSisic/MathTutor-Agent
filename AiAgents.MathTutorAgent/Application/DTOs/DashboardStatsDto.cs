namespace AiAgents.MathTutorAgent.Application.DTOs;

public class DashboardStatsDto
{
    public int TotalStudents { get; set; }
    public int ActiveSessions { get; set; }
    public int QuestionsCompleted { get; set; }
    public string AgentStatus { get; set; } = "Online";
}