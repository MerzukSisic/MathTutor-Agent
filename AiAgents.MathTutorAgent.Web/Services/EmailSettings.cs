namespace AiAgents.MathTutorAgent.Web.Services;

public class EmailSettings
{
    public bool Enabled { get; set; }
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "MathTutor AI";
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public bool UseSsl { get; set; } = true;
    public string SmtpUsername { get; set; } = string.Empty;
    public string SmtpPassword { get; set; } = string.Empty;
}
