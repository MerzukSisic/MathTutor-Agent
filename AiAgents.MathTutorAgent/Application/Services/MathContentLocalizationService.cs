using System.Text.RegularExpressions;
using AiAgents.MathTutorAgent.Application.DTOs;

namespace AiAgents.MathTutorAgent.Application.Services;

public class MathContentLocalizationService
{
    private static readonly Dictionary<string, string> TopicTranslationsBs = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Addition"] = "Sabiranje",
        ["Subtraction"] = "Oduzimanje",
        ["Multiplication"] = "Množenje",
        ["Division"] = "Dijeljenje",
        ["Fractions"] = "Razlomci",
        ["Decimals & Percentages"] = "Decimale i procenti",
        ["Negative Numbers"] = "Negativni brojevi",
        ["Algebraic Expressions"] = "Algebarski izrazi",
        ["Linear Equations"] = "Linearne jednačine",
        ["Inequalities"] = "Nejednačine",
        ["Systems of Equations"] = "Sistemi jednačina",
        ["Polynomials"] = "Polinomi",
        ["Set Basics"] = "Osnove skupova",
        ["Set Operations"] = "Operacije sa skupovima",
        ["Propositional Logic"] = "Propozicijska logika",
        ["Tautologies & Contradictions"] = "Tautologije i kontradikcije",
        ["Basic Shapes"] = "Osnovni oblici",
        ["Perimeter & Area"] = "Obim i površina",
        ["Triangles"] = "Trouglovi",
        ["Pythagorean Theorem"] = "Pitagorina teorema"
    };

    private static readonly Dictionary<string, string> AreaTranslationsBs = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Arithmetic"] = "Aritmetika",
        ["PreAlgebra"] = "Predalgebra",
        ["Algebra"] = "Algebra",
        ["Logic"] = "Logika",
        ["SetTheory"] = "Teorija skupova",
        ["Geometry"] = "Geometrija",
        ["Proofs"] = "Dokazi"
    };

    private static readonly Dictionary<string, string> ShapeTranslationsBs = new(StringComparer.OrdinalIgnoreCase)
    {
        ["triangle"] = "trougao",
        ["square"] = "kvadrat",
        ["rectangle"] = "pravougaonik",
        ["quadrilateral"] = "četverougao",
        ["pentagon"] = "petougao",
        ["hexagon"] = "šesterougao",
        ["heptagon"] = "sedmerougao",
        ["octagon"] = "osmerougao",
        ["nonagon"] = "deveterougao",
        ["decagon"] = "deseterougao",
        ["cube"] = "kocka",
        ["circle"] = "krug"
    };

    public string NormalizeLanguage(string? languageCode)
    {
        return string.Equals(languageCode, "en", StringComparison.OrdinalIgnoreCase) ? "en" : "bs";
    }

    public bool IsBosnian(string? languageCode) => NormalizeLanguage(languageCode) == "bs";

    public string LocalizeTopicName(string? topicName, string? languageCode)
    {
        if (!IsBosnian(languageCode) || string.IsNullOrWhiteSpace(topicName))
        {
            return topicName ?? string.Empty;
        }

        return TopicTranslationsBs.TryGetValue(topicName.Trim(), out var translated)
            ? translated
            : topicName;
    }

    public string LocalizeAreaName(string? areaName, string? languageCode)
    {
        if (!IsBosnian(languageCode) || string.IsNullOrWhiteSpace(areaName))
        {
            return areaName ?? string.Empty;
        }

        return AreaTranslationsBs.TryGetValue(areaName.Trim(), out var translated)
            ? translated
            : areaName;
    }

    public string LocalizeDecision(string? decision, string? languageCode)
    {
        if (!IsBosnian(languageCode) || string.IsNullOrWhiteSpace(decision))
        {
            return decision ?? string.Empty;
        }

        return decision.Trim().ToLowerInvariant() switch
        {
            "advance" => "Napreduj",
            "review" => "Ponovi",
            "remediate" => "Dodatna vježba",
            _ => decision
        };
    }

    public string LocalizeAnswerToken(string? answer, string? languageCode)
    {
        if (!IsBosnian(languageCode) || string.IsNullOrWhiteSpace(answer))
        {
            return answer ?? string.Empty;
        }

        var trimmed = answer.Trim();
        return trimmed.ToLowerInvariant() switch
        {
            "yes" => "Da",
            "no" => "Ne",
            "true" => "Tačno",
            "false" => "Netačno",
            _ => trimmed
        };
    }

    public string LocalizeQuestionText(string? questionText, string? languageCode)
    {
        if (!IsBosnian(languageCode) || string.IsNullOrWhiteSpace(questionText))
        {
            return questionText ?? string.Empty;
        }

        var text = questionText.Trim();

        // High-confidence full-template translations first (prevents mixed EN/BS text).
        text = Regex.Replace(text, @"^What\s+is\s+the\s+sum\s+of\s+angles\s+in\s+a\s+triangle\?$", "Koliki je zbir uglova u trouglu?", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"^What\s+shape\s+has\s+all\s+sides\s+equal\?$", "Koji oblik ima sve stranice jednake?", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"^What\s+is\s+the\s+power\s+set\s+of\s+(.+?)\?$", "Koji je partitivni skup od $1?", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"^Triangle\s+has\s+angles\s+(.+?)\s+and\s+(.+?)\.\s*What\s+is\s+the\s+third\s+angle\?$", "Trougao ima uglove $1 i $2. Koliki je treći ugao?", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"^Right\s+triangle\s+legs\s+(.+?)\s+and\s+(.+?)\.\s*Hypotenuse\?$", "Pravougli trougao ima katete $1 i $2. Kolika je hipotenuza?", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"^Complete\s+the\s+proportion:\s*(.+)$", "Dovrši proporciju: $1", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"^Evaluate:\s*(.+)$", "Izračunaj: $1", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"^If\s+all\s+even\s+numbers\s+are\s+divisible\s+by\s+2\s+and\s+n\s+is\s+even,\s*is\s+n\s+divisible\s+by\s+2\?$", "Ako su svi parni brojevi djeljivi sa 2 i n je paran, da li je n djeljiv sa 2?", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"^In\s+proof\s+by\s+contradiction,\s*do\s+we\s+assume\s+the\s+opposite\s+of\s+the\s+claim\s+first\?$", "U dokazu kontradikcijom, da li prvo pretpostavljamo suprotno od tvrdnje?", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"^Is\s+p\s*∨\s*¬p\s+always\s+true\?$", "Da li je p ∨ ¬p uvijek tačno?", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"^What\s+is\s+True\s*∧\s*False\?$", "Koliko je Tačno ∧ Netačno?", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"^What\s+is\s+True\s*∨\s*False\?$", "Koliko je Tačno ∨ Netačno?", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"^What\s+is\s+¬False\?$", "Koliko je ¬Netačno?", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"^Is\s+(.+?)\?$", "Da li je $1?", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"^How\s+many\s+sides\s+does\s+(?:an?\s+|the\s+)?(.+?)\s+have\?$", "Koliko stranica ima $1?", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"^How\s+many\s+vertices\s+does\s+(?:an?\s+|the\s+)?(.+?)\s+have\?$", "Koliko vrhova ima $1?", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"^What\s+is\s+(.+?)\?$", "Koliko je $1?", RegexOptions.IgnoreCase);

        text = Regex.Replace(text, @"^Perimeter\s+of\s+(.+?)\?$", "Koliki je obim za $1?", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"^Area\s+of\s+(.+?)\?$", "Kolika je površina za $1?", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"^Circumference\s+of\s+(.+?)\?$", "Koliki je obim kruga za $1?", RegexOptions.IgnoreCase);

        text = text.Replace("Simplify:", "Pojednostavi:", StringComparison.OrdinalIgnoreCase);
        text = text.Replace("Expand:", "Razvij:", StringComparison.OrdinalIgnoreCase);
        text = text.Replace("Factor:", "Faktoriši:", StringComparison.OrdinalIgnoreCase);
        text = text.Replace("Solve:", "Riješi:", StringComparison.OrdinalIgnoreCase);
        text = text.Replace("Solve for x:", "Riješi po x:", StringComparison.OrdinalIgnoreCase);
        text = text.Replace("Increase", "Povećaj", StringComparison.OrdinalIgnoreCase);
        text = text.Replace("Distance between", "Udaljenost između", StringComparison.OrdinalIgnoreCase);
        text = text.Replace("Right triangle", "Pravougli trougao", StringComparison.OrdinalIgnoreCase);
        text = text.Replace("hypotenuse", "hipotenuza", StringComparison.OrdinalIgnoreCase);

        text = text.Replace("sum of angles", "zbir uglova", StringComparison.OrdinalIgnoreCase);
        text = text.Replace("all sides equal", "sve stranice jednake", StringComparison.OrdinalIgnoreCase);
        text = text.Replace("third", "treći ugao", StringComparison.OrdinalIgnoreCase);
        text = text.Replace("power set", "partitivni skup", StringComparison.OrdinalIgnoreCase);
        text = text.Replace("What shape has", "Koji oblik ima", StringComparison.OrdinalIgnoreCase);
        text = text.Replace("What type is", "Koji tip je", StringComparison.OrdinalIgnoreCase);
        text = text.Replace("What is the", "Koliko je", StringComparison.OrdinalIgnoreCase);
        text = text.Replace("What is", "Koliko je", StringComparison.OrdinalIgnoreCase);
        text = text.Replace("How many", "Koliko", StringComparison.OrdinalIgnoreCase);
        text = text.Replace("always true", "uvijek tačno", StringComparison.OrdinalIgnoreCase);
        text = text.Replace("true", "tačno", StringComparison.OrdinalIgnoreCase);
        text = text.Replace("false", "netačno", StringComparison.OrdinalIgnoreCase);
        text = text.Replace(" and ", " i ", StringComparison.OrdinalIgnoreCase);
        text = text.Replace(" of ", " od ", StringComparison.OrdinalIgnoreCase);
        text = text.Replace(" with ", " sa ", StringComparison.OrdinalIgnoreCase);
        text = text.Replace(" has ", " ima ", StringComparison.OrdinalIgnoreCase);
        text = text.Replace(" have ", " ima ", StringComparison.OrdinalIgnoreCase);
        text = text.Replace("the sum of", "zbir", StringComparison.OrdinalIgnoreCase);
        text = text.Replace("in a", "u", StringComparison.OrdinalIgnoreCase);
        text = text.Replace("in an", "u", StringComparison.OrdinalIgnoreCase);
        text = text.Replace("in the", "u", StringComparison.OrdinalIgnoreCase);

        foreach (var shape in ShapeTranslationsBs)
        {
            text = Regex.Replace(
                text,
                $@"\b{Regex.Escape(shape.Key)}\b",
                shape.Value,
                RegexOptions.IgnoreCase);
        }

        text = Regex.Replace(text, @"\bYes\b", "Da", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"\bNo\b", "Ne", RegexOptions.IgnoreCase);

        // Basic grammar cleanups for common shapes after Bosnian prepositions.
        text = Regex.Replace(text, @"\bu\s+trougao\b", "u trouglu", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"\bu\s+kvadrat\b", "u kvadratu", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"\bu\s+pravougaonik\b", "u pravougaoniku", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"\bthe\b", string.Empty, RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"\bdoes\b", string.Empty, RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"\bdo\b", string.Empty, RegexOptions.IgnoreCase);

        // Cleanup after replacements.
        text = Regex.Replace(text, @"\s+", " ");
        text = text.Replace(" ?",
            "?",
            StringComparison.Ordinal);

        return text.Trim();
    }

    public string LocalizeExplanationText(string? text, string? languageCode)
    {
        if (!IsBosnian(languageCode) || string.IsNullOrWhiteSpace(text))
        {
            return text ?? string.Empty;
        }

        var value = text;

        var replacements = new (string En, string Bs)[]
        {
            ("A triangle has", "Trougao ima"),
            ("A square has", "Kvadrat ima"),
            ("A cube has", "Kocka ima"),
            ("Count the straight edges", "Prebroj prave ivice"),
            ("Vertices are the corner points", "Vrhovi su ugaone tačke"),
            ("Addition combines numbers", "Sabiranje spaja brojeve"),
            ("Subtraction finds the difference", "Oduzimanje pronalazi razliku"),
            ("Multiplication is repeated addition", "Množenje je ponovljeno sabiranje"),
            ("Division splits a number into equal parts", "Dijeljenje dijeli broj na jednake dijelove"),
            ("Fractions show parts of a whole", "Razlomci predstavljaju dijelove cjeline"),
            ("Percent means 'per hundred'", "Procenat znači 'na sto'"),
            ("To solve equations", "Za rješavanje jednačina"),
            ("To simplify expressions", "Za pojednostavljenje izraza"),
            ("To expand, use FOIL", "Za razvijanje koristi FOIL"),
            ("Pythagorean theorem", "Pitagorina teorema"),
            ("Perimeter is the distance around a shape", "Obim je dužina ivice oko oblika"),
            ("Area is the space inside a shape", "Površina je prostor unutar oblika"),
            ("Break the problem into smaller steps", "Razloži zadatak na manje korake")
        };

        foreach (var replacement in replacements)
        {
            value = value.Replace(replacement.En, replacement.Bs, StringComparison.OrdinalIgnoreCase);
        }

        value = value.Replace("Example:", "Primjer:", StringComparison.OrdinalIgnoreCase);

        return value;
    }

    public CrossMathMilestoneDto? LocalizeMilestone(CrossMathMilestoneDto? dto, string? languageCode)
    {
        if (dto == null || !IsBosnian(languageCode))
        {
            return dto;
        }

        var title = dto.Title;
        var subtitle = dto.Subtitle;

        title = title switch
        {
            "Addition Challenge" => "Izazov sabiranja",
            "Subtraction Challenge" => "Izazov oduzimanja",
            "Multiplication Challenge" => "Izazov množenja",
            "Division Challenge" => "Izazov dijeljenja",
            "Chapter Challenge" => "Izazov poglavlja",
            "Grand CrossMath Final" => "Veliko CrossMath finale",
            _ => title
        };

        subtitle = subtitle switch
        {
            "Fill the missing numbers and solve each row equation." => "Popuni nedostajuće brojeve i riješi jednačinu u svakom redu.",
            "Mixed operations from all four chapters." => "Miješane operacije iz sva četiri poglavlja.",
            _ => subtitle
        };

        return new CrossMathMilestoneDto
        {
            ChallengeKey = dto.ChallengeKey,
            Title = title,
            Subtitle = subtitle,
            Mode = dto.Mode,
            Size = dto.Size
        };
    }
}
