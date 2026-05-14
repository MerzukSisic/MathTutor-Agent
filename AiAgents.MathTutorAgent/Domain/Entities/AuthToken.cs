namespace AiAgents.MathTutorAgent.Domain.Entities;

public class AuthToken
{
    public int Id { get; set; }
    public int UserAccountId { get; set; }
    public string Purpose { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? ConsumedAtUtc { get; set; }

    public UserAccount UserAccount { get; set; } = null!;
}

public static class AuthTokenPurposes
{
    public const string EmailConfirmation = "EmailConfirmation";
    public const string PasswordReset = "PasswordReset";
}
