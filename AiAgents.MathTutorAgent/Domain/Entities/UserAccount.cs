namespace AiAgents.MathTutorAgent.Domain.Entities;

public class UserAccount
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = UserRoles.Student;
    public bool EmailConfirmed { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? StudentId { get; set; }

    public Student? Student { get; set; }
}

public static class UserRoles
{
    public const string Admin = "Admin";
    public const string Student = "Student";
}
