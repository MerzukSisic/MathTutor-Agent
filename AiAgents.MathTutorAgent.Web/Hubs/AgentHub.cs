using Microsoft.AspNetCore.SignalR;

namespace AiAgents.MathTutorAgent.Web.Hubs;

public class AgentHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var rawStudentId = Context.GetHttpContext()?.Request.Query["studentId"].ToString();
        if (int.TryParse(rawStudentId, out var studentId) && studentId > 0)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GetStudentGroup(studentId));
        }

        await base.OnConnectedAsync();
    }

    public static string GetStudentGroup(int studentId) => $"student-{studentId}";
}
