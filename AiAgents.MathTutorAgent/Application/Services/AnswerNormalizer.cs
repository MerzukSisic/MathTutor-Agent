using System.Text.RegularExpressions;

namespace AiAgents.MathTutorAgent.Application.Services;

/// <summary>
/// Normalizes answers to allow flexible input (e.g., "180" matches "180°")
/// </summary>
public static class AnswerNormalizer
{
    private static readonly Dictionary<string, string> SymbolReplacements = new()
    {
        // Unicode math symbols → ASCII equivalents
        { "×", "*" },
        { "÷", "/" },
        { "−", "-" }, // Unicode minus
        { "–", "-" }, // En dash
        { "—", "-" }, // Em dash
        
        // Remove symbols that are optional
        { "°", "" },  // Degrees
        { "√", "sqrt" },
        { "≈", "~" },
        { "≤", "<=" },
        { "≥", ">=" },
        
        // Set theory symbols (normalize to text)
        { "∈", "in" },
        { "∉", "notin" },
        { "⊆", "subset" },
        { "⊂", "subset" },
        { "∪", "union" },
        { "∩", "intersection" },
        { "∅", "empty" }
    };

    /// <summary>
    /// Normalize an answer for comparison
    /// </summary>
    public static string Normalize(string answer)
    {
        if (string.IsNullOrWhiteSpace(answer))
            return string.Empty;

        var normalized = answer.Trim();

        // 1. Replace special symbols
        foreach (var (symbol, replacement) in SymbolReplacements)
        {
            normalized = normalized.Replace(symbol, replacement);
        }

        // 2. Remove extra whitespace
        normalized = Regex.Replace(normalized, @"\s+", " ");

        // 3. Lowercase (for text comparisons)
        normalized = normalized.ToLowerInvariant();
        normalized = normalized.Trim(' ', '.', ',', '!', '?', ':', ';', '"', '\'');

        // 4. Canonicalize yes/no style answers across EN/BS variants
        normalized = normalized switch
        {
            "yes" or "y" or "da" or "d" or "true" or "tacno" or "tačno" => "yes",
            "no" or "n" or "ne" or "false" or "netacno" or "netačno" => "no",
            _ => normalized
        };

        return normalized;
    }

    /// <summary>
    /// Check if two answers are equivalent
    /// </summary>
    public static bool AreEquivalent(string correctAnswer, string studentAnswer)
    {
        var normalizedCorrect = Normalize(correctAnswer);
        var normalizedStudent = Normalize(studentAnswer);

        // Direct match
        if (normalizedCorrect == normalizedStudent)
            return true;

        // Try numeric comparison (handles "180" vs "180°")
        if (TryParseNumeric(normalizedCorrect, out var correctNum) &&
            TryParseNumeric(normalizedStudent, out var studentNum))
        {
            return Math.Abs(correctNum - studentNum) < 0.01; // Allow small rounding errors
        }

        // Try fraction comparison ("1/2" vs "0.5")
        if (TryParseFraction(normalizedCorrect, out var correctFrac) &&
            TryParseFraction(normalizedStudent, out var studentFrac))
        {
            return Math.Abs(correctFrac - studentFrac) < 0.01;
        }

        // Try alternative forms (e.g., "x > 4" vs "x>4")
        var compactCorrect = RemoveAllWhitespace(normalizedCorrect);
        var compactStudent = RemoveAllWhitespace(normalizedStudent);
        if (compactCorrect == compactStudent)
            return true;

        // Try sorting sets ("{1, 2, 3}" vs "{3, 2, 1}")
        if (AreEquivalentSets(normalizedCorrect, normalizedStudent))
            return true;

        return false;
    }

    private static bool TryParseNumeric(string value, out double result)
    {
        // Extract first number from string (e.g., "5√2 ≈ 7.07" → 7.07)
        var match = Regex.Match(value, @"[-+]?\d+\.?\d*");
        if (match.Success)
        {
            return double.TryParse(match.Value, out result);
        }

        result = 0;
        return false;
    }

    private static bool TryParseFraction(string value, out double result)
    {
        var match = Regex.Match(value, @"^(\d+)/(\d+)$");
        if (match.Success)
        {
            var numerator = int.Parse(match.Groups[1].Value);
            var denominator = int.Parse(match.Groups[2].Value);
            result = (double)numerator / denominator;
            return true;
        }

        result = 0;
        return false;
    }

    private static string RemoveAllWhitespace(string value)
    {
        return Regex.Replace(value, @"\s+", "");
    }

    private static bool AreEquivalentSets(string set1, string set2)
    {
        // Check if both strings look like sets: "{...}"
        if (!set1.StartsWith("{") || !set2.StartsWith("{"))
            return false;

        // Extract elements
        var elements1 = ExtractSetElements(set1);
        var elements2 = ExtractSetElements(set2);

        // Compare sorted
        return elements1.OrderBy(e => e).SequenceEqual(elements2.OrderBy(e => e));
    }

    private static List<string> ExtractSetElements(string set)
    {
        // Remove braces and split by comma
        var content = set.Trim('{', '}', ' ');
        return content.Split(',')
            .Select(e => e.Trim())
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .ToList();
    }
}
