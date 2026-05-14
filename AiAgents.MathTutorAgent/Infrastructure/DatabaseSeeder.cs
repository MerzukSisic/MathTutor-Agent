using AiAgents.MathTutorAgent.Application.Services;
using AiAgents.MathTutorAgent.Domain.Entities;
using AiAgents.MathTutorAgent.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AiAgents.MathTutorAgent.Infrastructure;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(MathTutorDbContext context)
    {
        if (await context.Topics.AnyAsync())
        {
            await EnsureExpandedCurriculumAsync(context);
            await EnsureDefaultAdminAccountAsync(context);
            return; // Core already seeded, only enrichment needed.
        }

        // ========== TOPICS (20+ covering full curriculum) ==========
        var topics = new List<Topic>
        {
            // ARITHMETIC (Topics 1-6)
            new() { Name = "Addition", Area = MathArea.Arithmetic, DifficultyBand = 1, Description = "Basic addition of integers" },
            new() { Name = "Subtraction", Area = MathArea.Arithmetic, DifficultyBand = 1, Description = "Basic subtraction" },
            new() { Name = "Multiplication", Area = MathArea.Arithmetic, DifficultyBand = 2, Description = "Multiplication tables and beyond" },
            new() { Name = "Division", Area = MathArea.Arithmetic, DifficultyBand = 2, Description = "Division and remainders" },
            new() { Name = "Fractions", Area = MathArea.Arithmetic, DifficultyBand = 3, Description = "Adding, subtracting, multiplying fractions" },
            new() { Name = "Decimals & Percentages", Area = MathArea.Arithmetic, DifficultyBand = 3, Description = "Decimal arithmetic and percentage calculations" },

            // ALGEBRA (Topics 7-12)
            new() { Name = "Negative Numbers", Area = MathArea.Algebra, DifficultyBand = 2, Description = "Operations with negative numbers" },
            new() { Name = "Algebraic Expressions", Area = MathArea.Algebra, DifficultyBand = 3, Description = "Simplifying and evaluating expressions" },
            new() { Name = "Linear Equations", Area = MathArea.Algebra, DifficultyBand = 4, Description = "Solving linear equations" },
            new() { Name = "Inequalities", Area = MathArea.Algebra, DifficultyBand = 4, Description = "Solving and graphing inequalities" },
            new() { Name = "Systems of Equations", Area = MathArea.Algebra, DifficultyBand = 5, Description = "Solving systems with substitution/elimination" },
            new() { Name = "Polynomials", Area = MathArea.Algebra, DifficultyBand = 5, Description = "Polynomial operations and factoring" },

            // LOGIC & SET THEORY (Topics 13-16)
            new() { Name = "Set Basics", Area = MathArea.SetTheory, DifficultyBand = 2, Description = "Set notation, membership, subsets" },
            new() { Name = "Set Operations", Area = MathArea.SetTheory, DifficultyBand = 3, Description = "Union, intersection, complement, difference" },
            new() { Name = "Propositional Logic", Area = MathArea.Logic, DifficultyBand = 3, Description = "Truth tables and logical operators" },
            new() { Name = "Tautologies & Contradictions", Area = MathArea.Logic, DifficultyBand = 4, Description = "Identifying tautologies and contradictions" },

            // GEOMETRY (Topics 17-20)
            new() { Name = "Basic Shapes", Area = MathArea.Geometry, DifficultyBand = 1, Description = "Identifying shapes and properties" },
            new() { Name = "Perimeter & Area", Area = MathArea.Geometry, DifficultyBand = 2, Description = "Calculating perimeter and area" },
            new() { Name = "Triangles", Area = MathArea.Geometry, DifficultyBand = 3, Description = "Triangle properties, types, and theorems" },
            new() { Name = "Pythagorean Theorem", Area = MathArea.Geometry, DifficultyBand = 4, Description = "Applying Pythagorean theorem" }
        };

        context.Topics.AddRange(topics);
        await context.SaveChangesAsync();

        // ========== TOPIC EDGES (DAG Prerequisites) ==========
        var edges = new List<TopicEdge>
        {
            // Arithmetic flow
            new() { PrerequisiteTopicId = topics[0].Id, DependentTopicId = topics[2].Id },
            new() { PrerequisiteTopicId = topics[1].Id, DependentTopicId = topics[3].Id },
            new() { PrerequisiteTopicId = topics[2].Id, DependentTopicId = topics[4].Id },
            new() { PrerequisiteTopicId = topics[3].Id, DependentTopicId = topics[4].Id },
            new() { PrerequisiteTopicId = topics[4].Id, DependentTopicId = topics[5].Id },

            // Algebra flow
            new() { PrerequisiteTopicId = topics[1].Id, DependentTopicId = topics[6].Id },
            new() { PrerequisiteTopicId = topics[4].Id, DependentTopicId = topics[7].Id },
            new() { PrerequisiteTopicId = topics[7].Id, DependentTopicId = topics[8].Id },
            new() { PrerequisiteTopicId = topics[8].Id, DependentTopicId = topics[9].Id },
            new() { PrerequisiteTopicId = topics[8].Id, DependentTopicId = topics[10].Id },
            new() { PrerequisiteTopicId = topics[7].Id, DependentTopicId = topics[11].Id },

            // Set Theory & Logic flow
            new() { PrerequisiteTopicId = topics[12].Id, DependentTopicId = topics[13].Id },
            new() { PrerequisiteTopicId = topics[14].Id, DependentTopicId = topics[15].Id },

            // Geometry flow
            new() { PrerequisiteTopicId = topics[16].Id, DependentTopicId = topics[17].Id },
            new() { PrerequisiteTopicId = topics[17].Id, DependentTopicId = topics[18].Id },
            new() { PrerequisiteTopicId = topics[18].Id, DependentTopicId = topics[19].Id }
        };

        context.TopicEdges.AddRange(edges);
        await context.SaveChangesAsync();

        // ========== QUESTIONS (150+) ==========
        var questions = new List<Question>();

        // TOPIC 1: Addition (10 questions, difficulty 1-2)
        questions.AddRange(new[]
        {
            new Question { TopicId = topics[0].Id, Difficulty = 1, QuestionText = "What is 5 + 3?", CorrectAnswer = "8", CommonMistakes = new List<string> { "7", "9" } },
            new Question { TopicId = topics[0].Id, Difficulty = 1, QuestionText = "What is 12 + 7?", CorrectAnswer = "19", CommonMistakes = new List<string> { "18", "20" } },
            new Question { TopicId = topics[0].Id, Difficulty = 1, QuestionText = "What is 8 + 6?", CorrectAnswer = "14", CommonMistakes = new List<string> { "13", "15" } },
            new Question { TopicId = topics[0].Id, Difficulty = 1, QuestionText = "What is 15 + 9?", CorrectAnswer = "24", CommonMistakes = new List<string> { "23", "25" } },
            new Question { TopicId = topics[0].Id, Difficulty = 1, QuestionText = "What is 11 + 11?", CorrectAnswer = "22", CommonMistakes = new List<string> { "21", "23" } },
            new Question { TopicId = topics[0].Id, Difficulty = 2, QuestionText = "What is 25 + 48?", CorrectAnswer = "73", CommonMistakes = new List<string> { "72", "74" } },
            new Question { TopicId = topics[0].Id, Difficulty = 2, QuestionText = "What is 67 + 89?", CorrectAnswer = "156", CommonMistakes = new List<string> { "155", "157" } },
            new Question { TopicId = topics[0].Id, Difficulty = 2, QuestionText = "What is 123 + 456?", CorrectAnswer = "579", CommonMistakes = new List<string> { "578", "580" } },
            new Question { TopicId = topics[0].Id, Difficulty = 2, QuestionText = "What is 234 + 567?", CorrectAnswer = "801", CommonMistakes = new List<string> { "800", "802" } },
            new Question { TopicId = topics[0].Id, Difficulty = 2, QuestionText = "What is 999 + 1?", CorrectAnswer = "1000", CommonMistakes = new List<string> { "999", "1001" } }
        });

        // TOPIC 2: Subtraction (10 questions, difficulty 1-2)
        questions.AddRange(new[]
        {
            new Question { TopicId = topics[1].Id, Difficulty = 1, QuestionText = "What is 10 - 4?", CorrectAnswer = "6", CommonMistakes = new List<string> { "5", "7" } },
            new Question { TopicId = topics[1].Id, Difficulty = 1, QuestionText = "What is 15 - 8?", CorrectAnswer = "7", CommonMistakes = new List<string> { "6", "8" } },
            new Question { TopicId = topics[1].Id, Difficulty = 1, QuestionText = "What is 20 - 13?", CorrectAnswer = "7", CommonMistakes = new List<string> { "6", "8" } },
            new Question { TopicId = topics[1].Id, Difficulty = 1, QuestionText = "What is 18 - 9?", CorrectAnswer = "9", CommonMistakes = new List<string> { "8", "10" } },
            new Question { TopicId = topics[1].Id, Difficulty = 1, QuestionText = "What is 25 - 15?", CorrectAnswer = "10", CommonMistakes = new List<string> { "9", "11" } },
            new Question { TopicId = topics[1].Id, Difficulty = 2, QuestionText = "What is 50 - 23?", CorrectAnswer = "27", CommonMistakes = new List<string> { "26", "28" } },
            new Question { TopicId = topics[1].Id, Difficulty = 2, QuestionText = "What is 100 - 47?", CorrectAnswer = "53", CommonMistakes = new List<string> { "52", "54" } },
            new Question { TopicId = topics[1].Id, Difficulty = 2, QuestionText = "What is 234 - 156?", CorrectAnswer = "78", CommonMistakes = new List<string> { "77", "79" } },
            new Question { TopicId = topics[1].Id, Difficulty = 2, QuestionText = "What is 500 - 238?", CorrectAnswer = "262", CommonMistakes = new List<string> { "261", "263" } },
            new Question { TopicId = topics[1].Id, Difficulty = 2, QuestionText = "What is 1000 - 999?", CorrectAnswer = "1", CommonMistakes = new List<string> { "0", "2" } }
        });

        // TOPIC 3: Multiplication (10 questions, difficulty 1-3)
        questions.AddRange(new[]
        {
            new Question { TopicId = topics[2].Id, Difficulty = 1, QuestionText = "What is 3 × 4?", CorrectAnswer = "12", CommonMistakes = new List<string> { "11", "13" } },
            new Question { TopicId = topics[2].Id, Difficulty = 1, QuestionText = "What is 6 × 7?", CorrectAnswer = "42", CommonMistakes = new List<string> { "41", "43" } },
            new Question { TopicId = topics[2].Id, Difficulty = 1, QuestionText = "What is 5 × 8?", CorrectAnswer = "40", CommonMistakes = new List<string> { "39", "41" } },
            new Question { TopicId = topics[2].Id, Difficulty = 1, QuestionText = "What is 9 × 9?", CorrectAnswer = "81", CommonMistakes = new List<string> { "80", "82" } },
            new Question { TopicId = topics[2].Id, Difficulty = 2, QuestionText = "What is 12 × 9?", CorrectAnswer = "108", CommonMistakes = new List<string> { "107", "109" } },
            new Question { TopicId = topics[2].Id, Difficulty = 2, QuestionText = "What is 15 × 7?", CorrectAnswer = "105", CommonMistakes = new List<string> { "104", "106" } },
            new Question { TopicId = topics[2].Id, Difficulty = 2, QuestionText = "What is 11 × 11?", CorrectAnswer = "121", CommonMistakes = new List<string> { "120", "122" } },
            new Question { TopicId = topics[2].Id, Difficulty = 3, QuestionText = "What is 23 × 14?", CorrectAnswer = "322", CommonMistakes = new List<string> { "321", "323" } },
            new Question { TopicId = topics[2].Id, Difficulty = 3, QuestionText = "What is 45 × 12?", CorrectAnswer = "540", CommonMistakes = new List<string> { "539", "541" } },
            new Question { TopicId = topics[2].Id, Difficulty = 3, QuestionText = "What is 99 × 99?", CorrectAnswer = "9801", CommonMistakes = new List<string> { "9800", "9802" } }
        });

        // TOPIC 4: Division (10 questions, difficulty 1-3)
        questions.AddRange(new[]
        {
            new Question { TopicId = topics[3].Id, Difficulty = 1, QuestionText = "What is 20 ÷ 4?", CorrectAnswer = "5", CommonMistakes = new List<string> { "4", "6" } },
            new Question { TopicId = topics[3].Id, Difficulty = 1, QuestionText = "What is 18 ÷ 3?", CorrectAnswer = "6", CommonMistakes = new List<string> { "5", "7" } },
            new Question { TopicId = topics[3].Id, Difficulty = 1, QuestionText = "What is 24 ÷ 6?", CorrectAnswer = "4", CommonMistakes = new List<string> { "3", "5" } },
            new Question { TopicId = topics[3].Id, Difficulty = 1, QuestionText = "What is 35 ÷ 5?", CorrectAnswer = "7", CommonMistakes = new List<string> { "6", "8" } },
            new Question { TopicId = topics[3].Id, Difficulty = 2, QuestionText = "What is 56 ÷ 8?", CorrectAnswer = "7", CommonMistakes = new List<string> { "6", "8" } },
            new Question { TopicId = topics[3].Id, Difficulty = 2, QuestionText = "What is 72 ÷ 9?", CorrectAnswer = "8", CommonMistakes = new List<string> { "7", "9" } },
            new Question { TopicId = topics[3].Id, Difficulty = 2, QuestionText = "What is 144 ÷ 12?", CorrectAnswer = "12", CommonMistakes = new List<string> { "11", "13" } },
            new Question { TopicId = topics[3].Id, Difficulty = 3, QuestionText = "What is 225 ÷ 15?", CorrectAnswer = "15", CommonMistakes = new List<string> { "14", "16" } },
            new Question { TopicId = topics[3].Id, Difficulty = 3, QuestionText = "What is 360 ÷ 12?", CorrectAnswer = "30", CommonMistakes = new List<string> { "29", "31" } },
            new Question { TopicId = topics[3].Id, Difficulty = 3, QuestionText = "What is 999 ÷ 3?", CorrectAnswer = "333", CommonMistakes = new List<string> { "332", "334" } }
        });

        // TOPIC 5: Fractions (12 questions, difficulty 1-4)
        questions.AddRange(new[]
        {
            new Question { TopicId = topics[4].Id, Difficulty = 1, QuestionText = "What is 1/2 of 10?", CorrectAnswer = "5", CommonMistakes = new List<string> { "4", "6" } },
            new Question { TopicId = topics[4].Id, Difficulty = 1, QuestionText = "What is 1/4 of 20?", CorrectAnswer = "5", CommonMistakes = new List<string> { "4", "6" } },
            new Question { TopicId = topics[4].Id, Difficulty = 1, QuestionText = "What is 1/3 of 12?", CorrectAnswer = "4", CommonMistakes = new List<string> { "3", "5" } },
            new Question { TopicId = topics[4].Id, Difficulty = 2, QuestionText = "What is 1/2 + 1/2?", CorrectAnswer = "1", CommonMistakes = new List<string> { "2/4", "1/2" } },
            new Question { TopicId = topics[4].Id, Difficulty = 2, QuestionText = "What is 1/4 + 1/4?", CorrectAnswer = "1/2", CommonMistakes = new List<string> { "2/8", "1/4" } },
            new Question { TopicId = topics[4].Id, Difficulty = 2, QuestionText = "What is 3/4 - 1/4?", CorrectAnswer = "1/2", CommonMistakes = new List<string> { "2/4", "1/4" } },
            new Question { TopicId = topics[4].Id, Difficulty = 3, QuestionText = "What is 1/2 + 1/4?", CorrectAnswer = "3/4", CommonMistakes = new List<string> { "2/6", "1/3" } },
            new Question { TopicId = topics[4].Id, Difficulty = 3, QuestionText = "What is 2/3 + 1/6?", CorrectAnswer = "5/6", CommonMistakes = new List<string> { "3/9", "4/6" } },
            new Question { TopicId = topics[4].Id, Difficulty = 3, QuestionText = "What is 2/3 × 3/4?", CorrectAnswer = "1/2", CommonMistakes = new List<string> { "6/12", "2/3" } },
            new Question { TopicId = topics[4].Id, Difficulty = 4, QuestionText = "What is 5/6 - 2/3?", CorrectAnswer = "1/6", CommonMistakes = new List<string> { "3/3", "1/3" } },
            new Question { TopicId = topics[4].Id, Difficulty = 4, QuestionText = "What is 3/4 ÷ 1/2?", CorrectAnswer = "3/2", CommonMistakes = new List<string> { "3/8", "6/4" } },
            new Question { TopicId = topics[4].Id, Difficulty = 4, QuestionText = "What is (1/2 + 1/3) × 2?", CorrectAnswer = "5/3", CommonMistakes = new List<string> { "4/6", "1" } }
        });

        // TOPIC 6: Decimals & Percentages (10 questions, difficulty 2-3)
        questions.AddRange(new[]
        {
            new Question { TopicId = topics[5].Id, Difficulty = 2, QuestionText = "What is 0.5 + 0.3?", CorrectAnswer = "0.8", CommonMistakes = new List<string> { "0.7", "0.9" } },
            new Question { TopicId = topics[5].Id, Difficulty = 2, QuestionText = "What is 1.2 - 0.7?", CorrectAnswer = "0.5", CommonMistakes = new List<string> { "0.4", "0.6" } },
            new Question { TopicId = topics[5].Id, Difficulty = 2, QuestionText = "What is 0.25 × 4?", CorrectAnswer = "1", CommonMistakes = new List<string> { "0.4", "1.25" } },
            new Question { TopicId = topics[5].Id, Difficulty = 2, QuestionText = "What is 50% of 100?", CorrectAnswer = "50", CommonMistakes = new List<string> { "25", "75" } },
            new Question { TopicId = topics[5].Id, Difficulty = 2, QuestionText = "What is 25% of 80?", CorrectAnswer = "20", CommonMistakes = new List<string> { "25", "40" } },
            new Question { TopicId = topics[5].Id, Difficulty = 3, QuestionText = "What is 15% of 200?", CorrectAnswer = "30", CommonMistakes = new List<string> { "15", "60" } },
            new Question { TopicId = topics[5].Id, Difficulty = 3, QuestionText = "What is 0.75 as a percentage?", CorrectAnswer = "75%", CommonMistakes = new List<string> { "0.75%", "7.5%" } },
            new Question { TopicId = topics[5].Id, Difficulty = 3, QuestionText = "What is 3/4 as a decimal?", CorrectAnswer = "0.75", CommonMistakes = new List<string> { "0.34", "0.43" } },
            new Question { TopicId = topics[5].Id, Difficulty = 3, QuestionText = "What is 0.6 as a fraction?", CorrectAnswer = "3/5", CommonMistakes = new List<string> { "6/10", "1/6" } },
            new Question { TopicId = topics[5].Id, Difficulty = 3, QuestionText = "Increase 50 by 20%", CorrectAnswer = "60", CommonMistakes = new List<string> { "70", "55" } }
        });

        // TOPIC 7: Negative Numbers (8 questions, difficulty 1-2)
        questions.AddRange(new[]
        {
            new Question { TopicId = topics[6].Id, Difficulty = 1, QuestionText = "What is 5 + (-3)?", CorrectAnswer = "2", CommonMistakes = new List<string> { "8", "-8" } },
            new Question { TopicId = topics[6].Id, Difficulty = 1, QuestionText = "What is -7 + 10?", CorrectAnswer = "3", CommonMistakes = new List<string> { "-3", "17" } },
            new Question { TopicId = topics[6].Id, Difficulty = 1, QuestionText = "What is -5 - 3?", CorrectAnswer = "-8", CommonMistakes = new List<string> { "-2", "8" } },
            new Question { TopicId = topics[6].Id, Difficulty = 2, QuestionText = "What is -4 × 3?", CorrectAnswer = "-12", CommonMistakes = new List<string> { "12", "-7" } },
            new Question { TopicId = topics[6].Id, Difficulty = 2, QuestionText = "What is -6 × (-2)?", CorrectAnswer = "12", CommonMistakes = new List<string> { "-12", "-8" } },
            new Question { TopicId = topics[6].Id, Difficulty = 2, QuestionText = "What is -20 ÷ 4?", CorrectAnswer = "-5", CommonMistakes = new List<string> { "5", "-4" } },
            new Question { TopicId = topics[6].Id, Difficulty = 2, QuestionText = "What is -15 ÷ (-3)?", CorrectAnswer = "5", CommonMistakes = new List<string> { "-5", "-12" } },
            new Question { TopicId = topics[6].Id, Difficulty = 2, QuestionText = "What is (-2)²?", CorrectAnswer = "4", CommonMistakes = new List<string> { "-4", "2" } }
        });

        // TOPIC 8: Algebraic Expressions (10 questions, difficulty 2-4)
        questions.AddRange(new[]
        {
            new Question { TopicId = topics[7].Id, Difficulty = 2, QuestionText = "Simplify: 2x + 3x", CorrectAnswer = "5x", CommonMistakes = new List<string> { "6x", "5x²" } },
            new Question { TopicId = topics[7].Id, Difficulty = 2, QuestionText = "Simplify: 5a - 2a", CorrectAnswer = "3a", CommonMistakes = new List<string> { "7a", "3" } },
            new Question { TopicId = topics[7].Id, Difficulty = 2, QuestionText = "Simplify: 4y + 2y - y", CorrectAnswer = "5y", CommonMistakes = new List<string> { "7y", "6y" } },
            new Question { TopicId = topics[7].Id, Difficulty = 3, QuestionText = "Simplify: 3(x + 2)", CorrectAnswer = "3x + 6", CommonMistakes = new List<string> { "3x + 2", "3x + 5" } },
            new Question { TopicId = topics[7].Id, Difficulty = 3, QuestionText = "Simplify: 2(a - 3) + 4", CorrectAnswer = "2a - 2", CommonMistakes = new List<string> { "2a - 3", "2a + 1" } },
            new Question { TopicId = topics[7].Id, Difficulty = 3, QuestionText = "Expand: (x + 3)(x + 2)", CorrectAnswer = "x² + 5x + 6", CommonMistakes = new List<string> { "x² + 6", "2x + 5" } },
            new Question { TopicId = topics[7].Id, Difficulty = 4, QuestionText = "Factor: x² + 5x + 6", CorrectAnswer = "(x + 2)(x + 3)", CommonMistakes = new List<string> { "(x + 1)(x + 6)", "x(x + 5) + 6" } },
            new Question { TopicId = topics[7].Id, Difficulty = 4, QuestionText = "Simplify: (2x)³", CorrectAnswer = "8x³", CommonMistakes = new List<string> { "2x³", "6x³" } },
            new Question { TopicId = topics[7].Id, Difficulty = 4, QuestionText = "Expand: (x - 4)²", CorrectAnswer = "x² - 8x + 16", CommonMistakes = new List<string> { "x² - 16", "x² + 16" } },
            new Question { TopicId = topics[7].Id, Difficulty = 4, QuestionText = "Simplify: x²y × xy²", CorrectAnswer = "x³y³", CommonMistakes = new List<string> { "x²y²", "x³y²" } }
        });

        // TOPIC 9: Linear Equations (10 questions, difficulty 3-5)
        questions.AddRange(new[]
        {
            new Question { TopicId = topics[8].Id, Difficulty = 3, QuestionText = "Solve: x + 5 = 12", CorrectAnswer = "7", CommonMistakes = new List<string> { "17", "8" } },
            new Question { TopicId = topics[8].Id, Difficulty = 3, QuestionText = "Solve: 2x = 10", CorrectAnswer = "5", CommonMistakes = new List<string> { "20", "8" } },
            new Question { TopicId = topics[8].Id, Difficulty = 3, QuestionText = "Solve: 3x - 4 = 11", CorrectAnswer = "5", CommonMistakes = new List<string> { "7", "15" } },
            new Question { TopicId = topics[8].Id, Difficulty = 4, QuestionText = "Solve: 2x + 5 = 15", CorrectAnswer = "5", CommonMistakes = new List<string> { "10", "20" } },
            new Question { TopicId = topics[8].Id, Difficulty = 4, QuestionText = "Solve: 5x - 7 = 18", CorrectAnswer = "5", CommonMistakes = new List<string> { "11", "25" } },
            new Question { TopicId = topics[8].Id, Difficulty = 4, QuestionText = "Solve: 3(x + 2) = 21", CorrectAnswer = "5", CommonMistakes = new List<string> { "7", "19" } },
            new Question { TopicId = topics[8].Id, Difficulty = 5, QuestionText = "Solve: 2(x - 3) + 4 = 12", CorrectAnswer = "7", CommonMistakes = new List<string> { "5", "9" } },
            new Question { TopicId = topics[8].Id, Difficulty = 5, QuestionText = "Solve: (x/2) + 3 = 7", CorrectAnswer = "8", CommonMistakes = new List<string> { "4", "10" } },
            new Question { TopicId = topics[8].Id, Difficulty = 5, QuestionText = "Solve: 5x - 2 = 3x + 8", CorrectAnswer = "5", CommonMistakes = new List<string> { "3", "10" } },
            new Question { TopicId = topics[8].Id, Difficulty = 5, QuestionText = "Solve: (2x + 1)/3 = 5", CorrectAnswer = "7", CommonMistakes = new List<string> { "14", "8" } }
        });

        // TOPIC 10: Inequalities (6 questions, difficulty 3-4)
        questions.AddRange(new[]
        {
            new Question { TopicId = topics[9].Id, Difficulty = 3, QuestionText = "Solve: x + 3 > 7", CorrectAnswer = "x > 4", CommonMistakes = new List<string> { "x > 10", "x < 4" } },
            new Question { TopicId = topics[9].Id, Difficulty = 3, QuestionText = "Solve: 2x ≤ 10", CorrectAnswer = "x ≤ 5", CommonMistakes = new List<string> { "x < 5", "x ≤ 20" } },
            new Question { TopicId = topics[9].Id, Difficulty = 4, QuestionText = "Solve: 3x - 5 ≥ 10", CorrectAnswer = "x ≥ 5", CommonMistakes = new List<string> { "x > 5", "x ≥ 15" } },
            new Question { TopicId = topics[9].Id, Difficulty = 4, QuestionText = "Solve: -2x < 6", CorrectAnswer = "x > -3", CommonMistakes = new List<string> { "x < -3", "x > 3" } },
            new Question { TopicId = topics[9].Id, Difficulty = 4, QuestionText = "Solve: 5 - x > 2", CorrectAnswer = "x < 3", CommonMistakes = new List<string> { "x > 3", "x < 7" } },
            new Question { TopicId = topics[9].Id, Difficulty = 4, QuestionText = "Solve: 2(x + 1) ≥ 8", CorrectAnswer = "x ≥ 3", CommonMistakes = new List<string> { "x ≥ 4", "x > 3" } }
        });

        // TOPIC 11: Systems of Equations (4 questions, difficulty 4-5)
        questions.AddRange(new[]
        {
            new Question { TopicId = topics[10].Id, Difficulty = 4, QuestionText = "Solve: x + y = 5, x - y = 1", CorrectAnswer = "x=3, y=2", CommonMistakes = new List<string> { "x=2, y=3" } },
            new Question { TopicId = topics[10].Id, Difficulty = 4, QuestionText = "Solve: 2x + y = 8, x - y = 1", CorrectAnswer = "x=3, y=2", CommonMistakes = new List<string> { "x=4, y=0" } },
            new Question { TopicId = topics[10].Id, Difficulty = 5, QuestionText = "Solve: 3x + 2y = 12, x + y = 5", CorrectAnswer = "x=2, y=3", CommonMistakes = new List<string> { "x=3, y=2" } },
            new Question { TopicId = topics[10].Id, Difficulty = 5, QuestionText = "Solve: 2x - 3y = 1, 4x + y = 9", CorrectAnswer = "x=2, y=1", CommonMistakes = new List<string> { "x=1, y=2" } }
        });

        // TOPIC 12: Polynomials (4 questions, difficulty 4-5)
        questions.AddRange(new[]
        {
            new Question { TopicId = topics[11].Id, Difficulty = 4, QuestionText = "Expand: (x + 3)(x - 2)", CorrectAnswer = "x² + x - 6", CommonMistakes = new List<string> { "x² - 6", "x² + 5x - 6" } },
            new Question { TopicId = topics[11].Id, Difficulty = 4, QuestionText = "Factor: x² - 9", CorrectAnswer = "(x + 3)(x - 3)", CommonMistakes = new List<string> { "(x - 9)(x + 1)", "x(x - 9)" } },
            new Question { TopicId = topics[11].Id, Difficulty = 5, QuestionText = "Expand: (2x + 1)(x - 3)", CorrectAnswer = "2x² - 5x - 3", CommonMistakes = new List<string> { "2x² - 3", "2x² - 6x - 3" } },
            new Question { TopicId = topics[11].Id, Difficulty = 5, QuestionText = "Factor: x² - 5x + 6", CorrectAnswer = "(x - 2)(x - 3)", CommonMistakes = new List<string> { "(x + 2)(x + 3)", "(x - 1)(x - 6)" } }
        });

        // TOPIC 13: Set Basics (8 questions, difficulty 1-2)
        questions.AddRange(new[]
        {
            new Question { TopicId = topics[12].Id, Difficulty = 1, QuestionText = "Is 3 ∈ {1, 2, 3, 4}?", CorrectAnswer = "Yes", CommonMistakes = new List<string> { "No" } },
            new Question { TopicId = topics[12].Id, Difficulty = 1, QuestionText = "Is 5 ∈ {1, 2, 3, 4}?", CorrectAnswer = "No", CommonMistakes = new List<string> { "Yes" } },
            new Question { TopicId = topics[12].Id, Difficulty = 1, QuestionText = "Is {1, 2} ⊆ {1, 2, 3}?", CorrectAnswer = "Yes", CommonMistakes = new List<string> { "No" } },
            new Question { TopicId = topics[12].Id, Difficulty = 2, QuestionText = "What is |{a, b, c}|?", CorrectAnswer = "3", CommonMistakes = new List<string> { "4", "2" } },
            new Question { TopicId = topics[12].Id, Difficulty = 2, QuestionText = "Is ∅ ⊆ {1, 2}?", CorrectAnswer = "Yes", CommonMistakes = new List<string> { "No" } },
            new Question { TopicId = topics[12].Id, Difficulty = 2, QuestionText = "Is {1, 2} = {2, 1}?", CorrectAnswer = "Yes", CommonMistakes = new List<string> { "No" } },
            new Question { TopicId = topics[12].Id, Difficulty = 2, QuestionText = "What is the power set of {a}?", CorrectAnswer = "{∅, {a}}", CommonMistakes = new List<string> { "{{a}}", "∅" } },
            new Question { TopicId = topics[12].Id, Difficulty = 2, QuestionText = "Is {1} ∈ {{1}, 2}?", CorrectAnswer = "Yes", CommonMistakes = new List<string> { "No" } }
        });

        // TOPIC 14: Set Operations (8 questions, difficulty 2-4)
        questions.AddRange(new[]
        {
            new Question { TopicId = topics[13].Id, Difficulty = 2, QuestionText = "What is {1, 2} ∪ {2, 3}?", CorrectAnswer = "{1, 2, 3}", CommonMistakes = new List<string> { "{2}", "{1, 2, 2, 3}" } },
            new Question { TopicId = topics[13].Id, Difficulty = 2, QuestionText = "What is {1, 2} ∩ {2, 3}?", CorrectAnswer = "{2}", CommonMistakes = new List<string> { "{1, 2, 3}", "∅" } },
            new Question { TopicId = topics[13].Id, Difficulty = 3, QuestionText = "What is {1, 2, 3} - {2}?", CorrectAnswer = "{1, 3}", CommonMistakes = new List<string> { "{2}", "{1, 2, 3}" } },
            new Question { TopicId = topics[13].Id, Difficulty = 3, QuestionText = "What is {1, 2} × {a, b}?", CorrectAnswer = "{(1,a), (1,b), (2,a), (2,b)}", CommonMistakes = new List<string> { "{1, 2, a, b}" } },
            new Question { TopicId = topics[13].Id, Difficulty = 3, QuestionText = "If U = {1,2,3,4}, what is {1,2}'?", CorrectAnswer = "{3, 4}", CommonMistakes = new List<string> { "{1, 2}", "U" } },
            new Question { TopicId = topics[13].Id, Difficulty = 4, QuestionText = "What is ({1,2} ∪ {3}) ∩ {2,3}?", CorrectAnswer = "{2, 3}", CommonMistakes = new List<string> { "{1, 2, 3}", "{3}" } },
            new Question { TopicId = topics[13].Id, Difficulty = 4, QuestionText = "De Morgan: ({1,2} ∪ {3})' = ?", CorrectAnswer = "{1,2}' ∩ {3}'", CommonMistakes = new List<string> { "{1,2}' ∪ {3}'" } },
            new Question { TopicId = topics[13].Id, Difficulty = 4, QuestionText = "Is A ∪ (B ∩ C) = (A ∪ B) ∩ (A ∪ C)?", CorrectAnswer = "Yes", CommonMistakes = new List<string> { "No" } }
        });

        // TOPIC 15: Propositional Logic (8 questions, difficulty 2-4)
        questions.AddRange(new[]
        {
            new Question { TopicId = topics[14].Id, Difficulty = 2, QuestionText = "What is ¬True?", CorrectAnswer = "False", CommonMistakes = new List<string> { "True" } },
            new Question { TopicId = topics[14].Id, Difficulty = 2, QuestionText = "What is True ∧ False?", CorrectAnswer = "False", CommonMistakes = new List<string> { "True" } },
            new Question { TopicId = topics[14].Id, Difficulty = 2, QuestionText = "What is True ∨ False?", CorrectAnswer = "True", CommonMistakes = new List<string> { "False" } },
            new Question { TopicId = topics[14].Id, Difficulty = 3, QuestionText = "What is False → True?", CorrectAnswer = "True", CommonMistakes = new List<string> { "False" } },
            new Question { TopicId = topics[14].Id, Difficulty = 3, QuestionText = "What is True → False?", CorrectAnswer = "False", CommonMistakes = new List<string> { "True" } },
            new Question { TopicId = topics[14].Id, Difficulty = 3, QuestionText = "What is True ↔ True?", CorrectAnswer = "True", CommonMistakes = new List<string> { "False" } },
            new Question { TopicId = topics[14].Id, Difficulty = 4, QuestionText = "Is (p → q) ≡ (¬p ∨ q)?", CorrectAnswer = "Yes", CommonMistakes = new List<string> { "No" } },
            new Question { TopicId = topics[14].Id, Difficulty = 4, QuestionText = "What is ¬(p ∧ q)?", CorrectAnswer = "¬p ∨ ¬q", CommonMistakes = new List<string> { "¬p ∧ ¬q" } }
        });

        // TOPIC 16: Tautologies (6 questions, difficulty 3-5)
        questions.AddRange(new[]
        {
            new Question { TopicId = topics[15].Id, Difficulty = 3, QuestionText = "Is p ∨ ¬p a tautology?", CorrectAnswer = "Yes", CommonMistakes = new List<string> { "No" } },
            new Question { TopicId = topics[15].Id, Difficulty = 3, QuestionText = "Is p ∧ ¬p a tautology?", CorrectAnswer = "No", CommonMistakes = new List<string> { "Yes" } },
            new Question { TopicId = topics[15].Id, Difficulty = 4, QuestionText = "Is (p → q) ∨ (q → p) a tautology?", CorrectAnswer = "Yes", CommonMistakes = new List<string> { "No" } },
            new Question { TopicId = topics[15].Id, Difficulty = 4, QuestionText = "Is p → (q → p) a tautology?", CorrectAnswer = "Yes", CommonMistakes = new List<string> { "No" } },
            new Question { TopicId = topics[15].Id, Difficulty = 5, QuestionText = "Is ((p → q) ∧ p) → q a tautology?", CorrectAnswer = "Yes", CommonMistakes = new List<string> { "No" } },
            new Question { TopicId = topics[15].Id, Difficulty = 5, QuestionText = "Is ((p → q) ∧ ¬q) → ¬p a tautology?", CorrectAnswer = "Yes", CommonMistakes = new List<string> { "No" } }
        });

        // TOPIC 17: Basic Shapes (6 questions, difficulty 1-2)
        questions.AddRange(new[]
        {
            new Question { TopicId = topics[16].Id, Difficulty = 1, QuestionText = "How many sides does a triangle have?", CorrectAnswer = "3", CommonMistakes = new List<string> { "4", "2" } },
            new Question { TopicId = topics[16].Id, Difficulty = 1, QuestionText = "How many sides does a square have?", CorrectAnswer = "4", CommonMistakes = new List<string> { "3", "5" } },
            new Question { TopicId = topics[16].Id, Difficulty = 1, QuestionText = "How many sides does a pentagon have?", CorrectAnswer = "5", CommonMistakes = new List<string> { "6", "4" } },
            new Question { TopicId = topics[16].Id, Difficulty = 2, QuestionText = "What is the sum of angles in a triangle?", CorrectAnswer = "180°", CommonMistakes = new List<string> { "360°", "90°" } },
            new Question { TopicId = topics[16].Id, Difficulty = 2, QuestionText = "How many vertices does a cube have?", CorrectAnswer = "8", CommonMistakes = new List<string> { "6", "12" } },
            new Question { TopicId = topics[16].Id, Difficulty = 2, QuestionText = "What shape has all sides equal?", CorrectAnswer = "Square", CommonMistakes = new List<string> { "Rectangle", "Rhombus" } }
        });

        // TOPIC 18: Perimeter & Area (8 questions, difficulty 2-3)
        questions.AddRange(new[]
        {
            new Question { TopicId = topics[17].Id, Difficulty = 2, QuestionText = "Perimeter of square with side 5?", CorrectAnswer = "20", CommonMistakes = new List<string> { "25", "10" } },
            new Question { TopicId = topics[17].Id, Difficulty = 2, QuestionText = "Area of square with side 5?", CorrectAnswer = "25", CommonMistakes = new List<string> { "20", "10" } },
            new Question { TopicId = topics[17].Id, Difficulty = 2, QuestionText = "Perimeter of rectangle 4×6?", CorrectAnswer = "20", CommonMistakes = new List<string> { "24", "10" } },
            new Question { TopicId = topics[17].Id, Difficulty = 2, QuestionText = "Area of rectangle 4×6?", CorrectAnswer = "24", CommonMistakes = new List<string> { "20", "10" } },
            new Question { TopicId = topics[17].Id, Difficulty = 3, QuestionText = "Area of triangle base=6, height=4?", CorrectAnswer = "12", CommonMistakes = new List<string> { "24", "10" } },
            new Question { TopicId = topics[17].Id, Difficulty = 3, QuestionText = "Area of circle with radius 3? (π≈3.14)", CorrectAnswer = "28.26", CommonMistakes = new List<string> { "18.84", "9.42" } },
            new Question { TopicId = topics[17].Id, Difficulty = 3, QuestionText = "Circumference of circle radius 5? (π≈3.14)", CorrectAnswer = "31.4", CommonMistakes = new List<string> { "78.5", "15.7" } },
            new Question { TopicId = topics[17].Id, Difficulty = 3, QuestionText = "Area of parallelogram base=8, height=5?", CorrectAnswer = "40", CommonMistakes = new List<string> { "26", "13" } }
        });

        // TOPIC 19: Triangles (6 questions, difficulty 3-4)
        questions.AddRange(new[]
        {
            new Question { TopicId = topics[18].Id, Difficulty = 3, QuestionText = "In triangle with angles 60° and 70°, what is the third?", CorrectAnswer = "50°", CommonMistakes = new List<string> { "40°", "60°" } },
            new Question { TopicId = topics[18].Id, Difficulty = 3, QuestionText = "What type is triangle with all sides equal?", CorrectAnswer = "Equilateral", CommonMistakes = new List<string> { "Isosceles", "Scalene" } },
            new Question { TopicId = topics[18].Id, Difficulty = 3, QuestionText = "What type has one 90° angle?", CorrectAnswer = "Right triangle", CommonMistakes = new List<string> { "Acute", "Obtuse" } },
            new Question { TopicId = topics[18].Id, Difficulty = 4, QuestionText = "Triangle sides 3, 4, x. What is max x?", CorrectAnswer = "< 7", CommonMistakes = new List<string> { "7", "8" } },
            new Question { TopicId = topics[18].Id, Difficulty = 4, QuestionText = "Exterior angle of triangle equals?", CorrectAnswer = "Sum of opposite interior angles", CommonMistakes = new List<string> { "180° - interior angle" } },
            new Question { TopicId = topics[18].Id, Difficulty = 4, QuestionText = "In isosceles triangle, base angles are?", CorrectAnswer = "Equal", CommonMistakes = new List<string> { "Different", "90°" } }
        });

        // TOPIC 20: Pythagorean Theorem (6 questions, difficulty 3-5)
        questions.AddRange(new[]
        {
            new Question { TopicId = topics[19].Id, Difficulty = 3, QuestionText = "Right triangle legs 3 and 4, hypotenuse?", CorrectAnswer = "5", CommonMistakes = new List<string> { "7", "6" } },
            new Question { TopicId = topics[19].Id, Difficulty = 3, QuestionText = "Right triangle legs 5 and 12, hypotenuse?", CorrectAnswer = "13", CommonMistakes = new List<string> { "17", "14" } },
            new Question { TopicId = topics[19].Id, Difficulty = 4, QuestionText = "Right triangle hypotenuse 10, one leg 6, other leg?", CorrectAnswer = "8", CommonMistakes = new List<string> { "4", "16" } },
            new Question { TopicId = topics[19].Id, Difficulty = 4, QuestionText = "Is triangle with sides 5, 12, 13 a right triangle?", CorrectAnswer = "Yes", CommonMistakes = new List<string> { "No" } },
            new Question { TopicId = topics[19].Id, Difficulty = 5, QuestionText = "Distance between (0,0) and (3,4)?", CorrectAnswer = "5", CommonMistakes = new List<string> { "7", "6" } },
            new Question { TopicId = topics[19].Id, Difficulty = 5, QuestionText = "Diagonal of square with side 5?", CorrectAnswer = "5√2 ≈ 7.07", CommonMistakes = new List<string> { "10", "5" } }
        });

        context.Questions.AddRange(questions);
        await context.SaveChangesAsync();

        // ========== SYSTEM SETTINGS ==========
        context.SystemSettings.Add(new SystemSettings
        {
            MasteryAdvanceThreshold = 85f,
            MasteryReviewThreshold = 60f,
            PrerequisiteLockThreshold = 75f,
            ForgettingRiskThreshold = 0.6f,
            RevisionIntervalDays = 7
        });
        await context.SaveChangesAsync();

        // ========== KNOWLEDGE BASE (sample data) ==========
        var documents = new List<KnowledgeDocument>
        {
            new() { Title = "Arithmetic Fundamentals", Author = "Math Department", FilePath = "/kb/arithmetic.pdf", UploadedAt = DateTime.UtcNow },
            new() { Title = "Algebra Basics", Author = "Math Department", FilePath = "/kb/algebra.pdf", UploadedAt = DateTime.UtcNow },
            new() { Title = "Logic & Set Theory Guide", Author = "Math Department", FilePath = "/kb/logic-sets.pdf", UploadedAt = DateTime.UtcNow },
            new() { Title = "Geometry Essentials", Author = "Math Department", FilePath = "/kb/geometry.pdf", UploadedAt = DateTime.UtcNow }
        };

        context.KnowledgeDocuments.AddRange(documents);
        await context.SaveChangesAsync();

        context.KnowledgeChunks.AddRange(
            new KnowledgeChunk { DocumentId = documents[0].Id, PageNumber = 1, ChunkText = "Addition is the process of combining two or more numbers to get a sum.", Tags = new List<string> { "Addition", "Arithmetic" } },
            new KnowledgeChunk { DocumentId = documents[0].Id, PageNumber = 2, ChunkText = "Subtraction is the process of finding the difference between two numbers.", Tags = new List<string> { "Subtraction", "Arithmetic" } },
            new KnowledgeChunk { DocumentId = documents[0].Id, PageNumber = 3, ChunkText = "Multiplication is repeated addition. For example, 3 × 4 means adding 3 four times.", Tags = new List<string> { "Multiplication", "Arithmetic" } },
            new KnowledgeChunk { DocumentId = documents[0].Id, PageNumber = 4, ChunkText = "Fractions represent parts of a whole. The numerator shows how many parts, the denominator shows the total parts.", Tags = new List<string> { "Fractions", "Arithmetic" } },
            new KnowledgeChunk { DocumentId = documents[1].Id, PageNumber = 1, ChunkText = "An algebraic expression contains variables, constants, and operators.", Tags = new List<string> { "Expressions", "Algebra" } },
            new KnowledgeChunk { DocumentId = documents[1].Id, PageNumber = 2, ChunkText = "To solve a linear equation, isolate the variable by performing inverse operations on both sides.", Tags = new List<string> { "Equations", "Algebra" } },
            new KnowledgeChunk { DocumentId = documents[1].Id, PageNumber = 3, ChunkText = "The distributive property: a(b + c) = ab + ac", Tags = new List<string> { "Expressions", "Algebra", "Properties" } },
            new KnowledgeChunk { DocumentId = documents[2].Id, PageNumber = 1, ChunkText = "A set is a collection of distinct objects. Elements are denoted with ∈ symbol.", Tags = new List<string> { "Sets", "SetTheory" } },
            new KnowledgeChunk { DocumentId = documents[2].Id, PageNumber = 2, ChunkText = "Set union (∪) combines elements from both sets. Set intersection (∩) finds common elements.", Tags = new List<string> { "Sets", "SetTheory", "Operations" } },
            new KnowledgeChunk { DocumentId = documents[2].Id, PageNumber = 3, ChunkText = "A tautology is a logical statement that is always true, regardless of truth values of its components.", Tags = new List<string> { "Logic", "Tautology" } },
            new KnowledgeChunk { DocumentId = documents[2].Id, PageNumber = 4, ChunkText = "De Morgan's Laws: ¬(p ∧ q) ≡ (¬p ∨ ¬q) and ¬(p ∨ q) ≡ (¬p ∧ ¬q)", Tags = new List<string> { "Logic", "Laws" } },
            new KnowledgeChunk { DocumentId = documents[3].Id, PageNumber = 1, ChunkText = "A triangle is a polygon with three sides and three angles. The sum of angles is always 180°.", Tags = new List<string> { "Triangles", "Geometry" } },
            new KnowledgeChunk { DocumentId = documents[3].Id, PageNumber = 2, ChunkText = "Pythagorean theorem: In a right triangle, a² + b² = c², where c is the hypotenuse.", Tags = new List<string> { "Pythagorean", "Geometry", "Triangles" } },
            new KnowledgeChunk { DocumentId = documents[3].Id, PageNumber = 3, ChunkText = "Area of a rectangle is length × width. Perimeter is 2(length + width).", Tags = new List<string> { "Area", "Perimeter", "Geometry" } }
        );
        await context.SaveChangesAsync();

        await EnsureExpandedCurriculumAsync(context);
        await EnsureDefaultAdminAccountAsync(context);
    }

    private static async Task EnsureExpandedCurriculumAsync(MathTutorDbContext context)
    {
        var existingTopics = await context.Topics.ToListAsync();

        Topic EnsureTopic(string name, MathArea area, int band, string description)
        {
            var found = existingTopics.FirstOrDefault(t => t.Name == name);
            if (found != null)
            {
                return found;
            }

            var topic = new Topic
            {
                Name = name,
                Area = area,
                DifficultyBand = band,
                Description = description
            };

            context.Topics.Add(topic);
            existingTopics.Add(topic);
            return topic;
        }

        var orderOps = EnsureTopic(
            "Order of Operations",
            MathArea.PreAlgebra,
            2,
            "Parentheses, multiplication/division, addition/subtraction order");

        var ratios = EnsureTopic(
            "Ratios and Proportions",
            MathArea.PreAlgebra,
            3,
            "Equivalent ratios and proportions");

        var directProof = EnsureTopic(
            "Direct Proof Basics",
            MathArea.Proofs,
            4,
            "Building proofs from givens to conclusion");

        var contradiction = EnsureTopic(
            "Proof by Contradiction Intro",
            MathArea.Proofs,
            5,
            "Assume opposite and derive contradiction");

        await context.SaveChangesAsync();

        var existingEdges = await context.TopicEdges
            .Select(e => new { e.PrerequisiteTopicId, e.DependentTopicId })
            .ToListAsync();

        void EnsureEdge(int prerequisiteId, int dependentId)
        {
            if (existingEdges.Any(e => e.PrerequisiteTopicId == prerequisiteId && e.DependentTopicId == dependentId))
            {
                return;
            }

            context.TopicEdges.Add(new TopicEdge
            {
                PrerequisiteTopicId = prerequisiteId,
                DependentTopicId = dependentId
            });

            existingEdges.Add(new { PrerequisiteTopicId = prerequisiteId, DependentTopicId = dependentId });
        }

        var fractions = existingTopics.FirstOrDefault(t => t.Name == "Fractions");
        var linearEq = existingTopics.FirstOrDefault(t => t.Name == "Linear Equations");
        var propositionalLogic = existingTopics.FirstOrDefault(t => t.Name == "Propositional Logic");

        if (fractions != null)
        {
            EnsureEdge(fractions.Id, ratios.Id);
        }

        if (ratios.Id > 0 && linearEq != null)
        {
            EnsureEdge(ratios.Id, linearEq.Id);
        }

        if (propositionalLogic != null)
        {
            EnsureEdge(propositionalLogic.Id, directProof.Id);
        }

        EnsureEdge(directProof.Id, contradiction.Id);

        await context.SaveChangesAsync();

        var existingQuestions = await context.Questions
            .Select(q => new { q.TopicId, q.QuestionText })
            .ToListAsync();

        void EnsureQuestion(int topicId, int difficulty, string text, string answer, List<string> mistakes)
        {
            if (existingQuestions.Any(q => q.TopicId == topicId && q.QuestionText == text))
            {
                return;
            }

            context.Questions.Add(new Question
            {
                TopicId = topicId,
                Difficulty = difficulty,
                QuestionText = text,
                CorrectAnswer = answer,
                CommonMistakes = mistakes
            });

            existingQuestions.Add(new { TopicId = topicId, QuestionText = text });
        }

        EnsureQuestion(orderOps.Id, 2, "Evaluate: 7 + 3 × 4", "19", new List<string> { "40", "28" });
        EnsureQuestion(orderOps.Id, 2, "Evaluate: (7 + 3) × 4", "40", new List<string> { "19", "28" });
        EnsureQuestion(orderOps.Id, 3, "Evaluate: 18 ÷ 3 + 2 × 5", "16", new List<string> { "50", "13" });
        EnsureQuestion(orderOps.Id, 3, "Evaluate: 6 + (12 ÷ 3) × 2", "14", new List<string> { "12", "10" });

        EnsureQuestion(ratios.Id, 3, "Complete: 2:3 = 10:x", "15", new List<string> { "12", "13" });
        EnsureQuestion(ratios.Id, 3, "If 4 pens cost 12 KM, how much do 10 pens cost?", "30", new List<string> { "24", "28" });
        EnsureQuestion(ratios.Id, 4, "Complete: 5:8 = x:40", "25", new List<string> { "20", "32" });
        EnsureQuestion(ratios.Id, 4, "If 3 workers finish a task in 12h, how long for 6 workers (same speed)?", "6", new List<string> { "24", "9" });

        EnsureQuestion(directProof.Id, 4, "If n is even, can n be written as 2k for some integer k?", "Yes", new List<string> { "No" });
        EnsureQuestion(directProof.Id, 4, "Given a|b and b|c, does a|c hold?", "Yes", new List<string> { "No" });
        EnsureQuestion(directProof.Id, 4, "If m and n are odd, is m+n even?", "Yes", new List<string> { "No" });

        EnsureQuestion(contradiction.Id, 5, "In proof by contradiction, do we assume the negation of the claim first?", "Yes", new List<string> { "No" });
        EnsureQuestion(contradiction.Id, 5, "Can contradiction method prove irrationality statements?", "Yes", new List<string> { "No" });
        EnsureQuestion(contradiction.Id, 5, "If assumption leads to impossibility, is original claim true?", "Yes", new List<string> { "No" });

        await context.SaveChangesAsync();
    }

    private static async Task EnsureDefaultAdminAccountAsync(MathTutorDbContext context)
    {
        var adminEmail = Environment.GetEnvironmentVariable("MATH_TUTOR_DEFAULT_ADMIN_EMAIL");
        if (string.IsNullOrWhiteSpace(adminEmail))
        {
            adminEmail = "admin@mathtutor.local";
        }
        else
        {
            adminEmail = adminEmail.Trim().ToLowerInvariant();
        }

        if (await context.UserAccounts.AnyAsync(u => u.Email == adminEmail))
        {
            return;
        }

        var adminPassword = Environment.GetEnvironmentVariable("MATH_TUTOR_DEFAULT_ADMIN_PASSWORD");
        if (string.IsNullOrWhiteSpace(adminPassword))
        {
            return;
        }

        var passwordHashingService = new PasswordHashingService();
        var admin = new UserAccount
        {
            FullName = "MathTutor Admin",
            Email = adminEmail,
            PasswordHash = passwordHashingService.HashPassword(adminPassword),
            Role = UserRoles.Admin,
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow
        };

        context.UserAccounts.Add(admin);
        await context.SaveChangesAsync();
    }
}
