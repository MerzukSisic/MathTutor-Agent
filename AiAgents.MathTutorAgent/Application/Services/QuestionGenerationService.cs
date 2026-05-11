using AiAgents.MathTutorAgent.Domain.Entities;
using AiAgents.MathTutorAgent.Domain.Enums;
using AiAgents.MathTutorAgent.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AiAgents.MathTutorAgent.Application.Services;

public class QuestionGenerationService(MathTutorDbContext context)
{
    private static Random Rng => Random.Shared;

    public async Task<Question?> GenerateQuestionAsync(int topicId, int targetDifficulty, CancellationToken ct = default)
    {
        var topic = await context.Topics.FirstOrDefaultAsync(t => t.Id == topicId, ct);
        if (topic == null)
        {
            return null;
        }

        var existingTexts = await context.Questions
            .Where(q => q.TopicId == topicId)
            .Select(q => q.QuestionText)
            .ToListAsync(ct);

        var existingSet = existingTexts
            .Select(NormalizeQuestionText)
            .ToHashSet();

        for (var i = 0; i < 16; i++)
        {
            var generated = GenerateForTopic(topic, targetDifficulty);
            if (generated == null)
            {
                return null;
            }

            var normalized = NormalizeQuestionText(generated.QuestionText);
            if (existingSet.Contains(normalized))
            {
                continue;
            }

            context.Questions.Add(generated);
            await context.SaveChangesAsync(ct);
            return generated;
        }

        return null;
    }

    private static Question? GenerateForTopic(Topic topic, int difficulty)
    {
        var name = topic.Name.ToLowerInvariant();

        if (name.Contains("addition")) return GenerateAddition(topic.Id, difficulty);
        if (name.Contains("subtraction")) return GenerateSubtraction(topic.Id, difficulty);
        if (name.Contains("multiplication")) return GenerateMultiplication(topic.Id, difficulty);
        if (name.Contains("division")) return GenerateDivision(topic.Id, difficulty);
        if (name.Contains("fraction")) return GenerateFractions(topic.Id, difficulty);
        if (name.Contains("decimal") || name.Contains("percentage")) return GenerateDecimalsPercentages(topic.Id, difficulty);
        if (name.Contains("negative")) return GenerateNegativeNumbers(topic.Id, difficulty);
        if (name.Contains("expression")) return GenerateExpressions(topic.Id, difficulty);
        if (name.Contains("linear equation")) return GenerateLinearEquation(topic.Id, difficulty);
        if (name.Contains("inequal")) return GenerateInequality(topic.Id, difficulty);
        if (name.Contains("system")) return GenerateSystem(topic.Id, difficulty);
        if (name.Contains("polynomial")) return GeneratePolynomial(topic.Id, difficulty);
        if (name.Contains("set basic")) return GenerateSetBasics(topic.Id, difficulty);
        if (name.Contains("set operation")) return GenerateSetOperation(topic.Id, difficulty);
        if (name.Contains("propositional logic")) return GenerateLogic(topic.Id, difficulty);
        if (name.Contains("tautolog") || name.Contains("contradiction")) return GenerateTautology(topic.Id, difficulty);
        if (name.Contains("basic shapes")) return GenerateBasicShapes(topic.Id, difficulty);
        if (name.Contains("perimeter") || name.Contains("area")) return GeneratePerimeterArea(topic.Id, difficulty);
        if (name.Contains("triangle")) return GenerateTriangles(topic.Id, difficulty);
        if (name.Contains("pythagorean")) return GeneratePythagorean(topic.Id, difficulty);
        if (name.Contains("ratio") || name.Contains("proportion")) return GenerateRatios(topic.Id, difficulty);
        if (name.Contains("order of operations")) return GenerateOrderOfOperations(topic.Id, difficulty);
        if (name.Contains("direct proof")) return GenerateProofBasics(topic.Id, difficulty);
        if (name.Contains("contradiction")) return GenerateProofContradiction(topic.Id, difficulty);

        return topic.Area switch
        {
            MathArea.Arithmetic => GenerateAddition(topic.Id, difficulty),
            MathArea.PreAlgebra => GenerateOrderOfOperations(topic.Id, difficulty),
            MathArea.Algebra => GenerateLinearEquation(topic.Id, difficulty),
            MathArea.SetTheory => GenerateSetOperation(topic.Id, difficulty),
            MathArea.Logic => GenerateLogic(topic.Id, difficulty),
            MathArea.Geometry => GeneratePerimeterArea(topic.Id, difficulty),
            MathArea.Proofs => GenerateProofBasics(topic.Id, difficulty),
            _ => null
        };
    }

    private static Question GenerateAddition(int topicId, int difficulty)
    {
        var (min, max) = GetRangeByDifficulty(difficulty);
        var a = Rng.Next(min, max + 1);
        var b = Rng.Next(min, max + 1);
        var answer = a + b;

        return NewQuestion(topicId, difficulty, $"What is {a} + {b}?", answer.ToString(), MistakesAround(answer));
    }

    private static Question GenerateSubtraction(int topicId, int difficulty)
    {
        var (min, max) = GetRangeByDifficulty(difficulty);
        var a = Rng.Next(min + 4, max + 8);
        var b = Rng.Next(min, Math.Max(min + 1, a));
        var answer = a - b;

        return NewQuestion(topicId, difficulty, $"What is {a} - {b}?", answer.ToString(), MistakesAround(answer));
    }

    private static Question GenerateMultiplication(int topicId, int difficulty)
    {
        var limit = difficulty switch
        {
            <= 1 => 12,
            2 => 16,
            3 => 24,
            4 => 36,
            _ => 48
        };

        var a = Rng.Next(2, limit + 1);
        var b = Rng.Next(2, limit + 1);
        var answer = a * b;

        return NewQuestion(topicId, difficulty, $"What is {a} × {b}?", answer.ToString(), MistakesAround(answer, Math.Max(2, a / 2)));
    }

    private static Question GenerateDivision(int topicId, int difficulty)
    {
        var divisorMax = difficulty switch
        {
            <= 1 => 10,
            2 => 14,
            3 => 18,
            4 => 24,
            _ => 30
        };

        var divisor = Rng.Next(2, divisorMax + 1);
        var quotient = Rng.Next(2, divisorMax + 1);
        var dividend = divisor * quotient;

        return NewQuestion(topicId, difficulty, $"What is {dividend} ÷ {divisor}?", quotient.ToString(), MistakesAround(quotient));
    }

    private static Question GenerateFractions(int topicId, int difficulty)
    {
        var den = Rng.Next(2, difficulty <= 2 ? 8 : 12);
        var n1 = Rng.Next(1, den);
        var n2 = Rng.Next(1, den);
        var sum = n1 + n2;

        if (sum >= den)
        {
            var whole = sum / den;
            var rem = sum % den;
            var answer = rem == 0 ? whole.ToString() : $"{whole} {rem}/{den}";
            return NewQuestion(topicId, difficulty, $"What is {n1}/{den} + {n2}/{den}?", answer, new List<string> { $"{sum}/{den}", $"{Math.Max(0, rem - 1)}/{den}" });
        }

        return NewQuestion(topicId, difficulty, $"What is {n1}/{den} + {n2}/{den}?", $"{sum}/{den}", new List<string> { $"{sum + 1}/{den}", $"{Math.Max(1, sum - 1)}/{den}" });
    }

    private static Question GenerateDecimalsPercentages(int topicId, int difficulty)
    {
        var baseValue = Rng.Next(20, 500);
        var percent = difficulty switch
        {
            <= 2 => new[] { 10, 20, 25, 50 }[Rng.Next(0, 4)],
            3 => new[] { 5, 12, 15, 30, 40 }[Rng.Next(0, 5)],
            _ => new[] { 7, 18, 22, 35 }[Rng.Next(0, 4)]
        };

        var answer = Math.Round(baseValue * percent / 100.0, 2);
        return NewQuestion(topicId, difficulty, $"What is {percent}% of {baseValue}?", answer.ToString("0.##"), MistakesAround(answer));
    }

    private static Question GenerateNegativeNumbers(int topicId, int difficulty)
    {
        var a = Rng.Next(2, 30);
        var b = Rng.Next(2, 30);
        var op = Rng.Next(0, 3);

        return op switch
        {
            0 => NewQuestion(topicId, difficulty, $"What is -{a} + {b}?", (b - a).ToString(), MistakesAround(b - a)),
            1 => NewQuestion(topicId, difficulty, $"What is -{a} - {b}?", (-(a + b)).ToString(), MistakesAround(-(a + b))),
            _ => NewQuestion(topicId, difficulty, $"What is -{a} × -{b}?", (a * b).ToString(), MistakesAround(a * b))
        };
    }

    private static Question GenerateExpressions(int topicId, int difficulty)
    {
        var k1 = Rng.Next(2, 9);
        var k2 = Rng.Next(2, 9);
        var variable = new[] { "x", "y", "a", "m" }[Rng.Next(0, 4)];
        var answer = k1 + k2;
        return NewQuestion(topicId, difficulty, $"Simplify: {k1}{variable} + {k2}{variable}", $"{answer}{variable}", new List<string> { $"{answer + 1}{variable}", $"{k1 * k2}{variable}" });
    }

    private static Question GenerateLinearEquation(int topicId, int difficulty)
    {
        var x = Rng.Next(-12, 25);
        var a = Rng.Next(2, 9);
        var b = Rng.Next(-15, 20);
        var c = a * x + b;

        return NewQuestion(topicId, difficulty, $"Solve: {a}x {(b >= 0 ? "+" : "-")} {Math.Abs(b)} = {c}", x.ToString(), MistakesAround(x));
    }

    private static Question GenerateInequality(int topicId, int difficulty)
    {
        var k = Rng.Next(2, 11);
        var b = Rng.Next(-10, 15);
        var x = Rng.Next(-10, 20);
        var c = k * x + b;

        return NewQuestion(topicId, difficulty, $"Solve: {k}x {(b >= 0 ? "+" : "-")} {Math.Abs(b)} ≥ {c}", $"x ≥ {x}", new List<string> { $"x > {x}", $"x ≤ {x}" });
    }

    private static Question GenerateSystem(int topicId, int difficulty)
    {
        var x = Rng.Next(1, 10);
        var y = Rng.Next(1, 10);

        var a1 = Rng.Next(1, 4);
        var b1 = Rng.Next(1, 4);
        var a2 = Rng.Next(1, 4);
        var b2 = Rng.Next(1, 4);

        if (a1 * b2 == a2 * b1)
        {
            b2 += 1;
        }

        var c1 = a1 * x + b1 * y;
        var c2 = a2 * x + b2 * y;

        return NewQuestion(topicId, difficulty, $"Solve: {a1}x + {b1}y = {c1}, {a2}x + {b2}y = {c2}", $"x={x}, y={y}", new List<string> { $"x={y}, y={x}" });
    }

    private static Question GeneratePolynomial(int topicId, int difficulty)
    {
        var a = Rng.Next(2, 8);
        var b = Rng.Next(2, 8);
        return NewQuestion(topicId, difficulty, $"Expand: (x + {a})(x + {b})", $"x² + {a + b}x + {a * b}", new List<string> { $"x² + {a * b}x + {a + b}", $"x² + {a + b}x - {a * b}" });
    }

    private static Question GenerateSetBasics(int topicId, int difficulty)
    {
        var n = Rng.Next(2, 9);
        return NewQuestion(topicId, difficulty, $"Is {n} ∈ {{1, 2, 3, 4, 5}}?", n <= 5 ? "Yes" : "No", new List<string> { n <= 5 ? "No" : "Yes" });
    }

    private static Question GenerateSetOperation(int topicId, int difficulty)
    {
        var a = Rng.Next(1, 6);
        var b = a + Rng.Next(1, 4);
        var c = b + Rng.Next(1, 4);
        return NewQuestion(topicId, difficulty, $"What is {{{a}, {b}}} ∪ {{{b}, {c}}}?", $"{{{a}, {b}, {c}}}", new List<string> { $"{{{b}}}", $"{{{a}, {c}}}" });
    }

    private static Question GenerateLogic(int topicId, int difficulty)
    {
        var options = new[]
        {
            ("What is True ∧ False?", "False", new List<string> { "True" }),
            ("What is True ∨ False?", "True", new List<string> { "False" }),
            ("What is ¬False?", "True", new List<string> { "False" })
        };

        var selected = options[Rng.Next(0, options.Length)];
        return NewQuestion(topicId, difficulty, selected.Item1, selected.Item2, selected.Item3);
    }

    private static Question GenerateTautology(int topicId, int difficulty)
    {
        return NewQuestion(topicId, difficulty, "Is p ∨ ¬p always true?", "Yes", new List<string> { "No" });
    }

    private static Question GenerateBasicShapes(int topicId, int difficulty)
    {
        var sides = Rng.Next(3, 8);
        var name = sides switch
        {
            3 => "triangle",
            4 => "quadrilateral",
            5 => "pentagon",
            6 => "hexagon",
            _ => "heptagon"
        };

        return NewQuestion(topicId, difficulty, $"How many sides does a {name} have?", sides.ToString(), MistakesAround(sides));
    }

    private static Question GeneratePerimeterArea(int topicId, int difficulty)
    {
        var w = Rng.Next(2, 15);
        var h = Rng.Next(2, 15);
        if (Rng.Next(0, 2) == 0)
        {
            var p = 2 * (w + h);
            return NewQuestion(topicId, difficulty, $"Perimeter of rectangle {w}×{h}?", p.ToString(), MistakesAround(p));
        }

        var a = w * h;
        return NewQuestion(topicId, difficulty, $"Area of rectangle {w}×{h}?", a.ToString(), MistakesAround(a));
    }

    private static Question GenerateTriangles(int topicId, int difficulty)
    {
        var first = Rng.Next(20, 90);
        var second = Rng.Next(20, 90 - first);
        var third = 180 - first - second;

        return NewQuestion(topicId, difficulty, $"Triangle has angles {first}° and {second}°. What is the third angle?", $"{third}°", new List<string> { $"{third - 10}°", $"{third + 10}°" });
    }

    private static Question GeneratePythagorean(int topicId, int difficulty)
    {
        var triples = new (int a, int b, int c)[]
        {
            (3, 4, 5),
            (5, 12, 13),
            (8, 15, 17),
            (7, 24, 25)
        };

        var t = triples[Rng.Next(0, triples.Length)];
        return NewQuestion(topicId, difficulty, $"Right triangle legs {t.a} and {t.b}. Hypotenuse?", t.c.ToString(), MistakesAround(t.c));
    }

    private static Question GenerateRatios(int topicId, int difficulty)
    {
        var a = Rng.Next(1, 8);
        var b = Rng.Next(1, 8);
        var factor = Rng.Next(2, 7);
        var left = a * factor;
        var right = b * factor;

        return NewQuestion(topicId, difficulty, $"Complete the proportion: {a}:{b} = {left}:x", right.ToString(), MistakesAround(right));
    }

    private static Question GenerateOrderOfOperations(int topicId, int difficulty)
    {
        var a = Rng.Next(2, 15);
        var b = Rng.Next(2, 15);
        var c = Rng.Next(2, 15);
        var answer = a + b * c;

        return NewQuestion(topicId, difficulty, $"Evaluate: {a} + {b} × {c}", answer.ToString(), new List<string> { ((a + b) * c).ToString(), (a * b + c).ToString() });
    }

    private static Question GenerateProofBasics(int topicId, int difficulty)
    {
        return NewQuestion(topicId, difficulty, "If all even numbers are divisible by 2 and n is even, is n divisible by 2?", "Yes", new List<string> { "No" });
    }

    private static Question GenerateProofContradiction(int topicId, int difficulty)
    {
        return NewQuestion(topicId, difficulty, "In proof by contradiction, do we assume the opposite of the claim first?", "Yes", new List<string> { "No" });
    }

    private static Question NewQuestion(int topicId, int difficulty, string text, string answer, List<string> mistakes)
    {
        return new Question
        {
            TopicId = topicId,
            Difficulty = Math.Clamp(difficulty, 1, 5),
            QuestionText = text,
            CorrectAnswer = answer,
            CommonMistakes = mistakes
        };
    }

    private static List<string> MistakesAround(int value, int step = 1)
    {
        return new List<string>
        {
            (value - step).ToString(),
            (value + step).ToString()
        };
    }

    private static List<string> MistakesAround(double value)
    {
        return new List<string>
        {
            Math.Round(value * 0.9, 2).ToString("0.##"),
            Math.Round(value * 1.1, 2).ToString("0.##")
        };
    }

    private static string NormalizeQuestionText(string input)
    {
        return input.Trim().ToLowerInvariant();
    }

    private static (int min, int max) GetRangeByDifficulty(int difficulty)
    {
        return difficulty switch
        {
            <= 1 => (1, 20),
            2 => (10, 60),
            3 => (20, 120),
            4 => (40, 250),
            _ => (80, 500)
        };
    }
}
