namespace AiAgents.MathTutorAgent.Application.Services;

public static class StringNormalizer
{
    public static string NormalizeEmail(string email)
        => email.Trim().ToLowerInvariant();

    public static string NormalizeKey(string value)
        => value.Trim().ToLowerInvariant();
}
