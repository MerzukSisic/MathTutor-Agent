using AiAgents.MathTutorAgent.Domain.Entities;
using AiAgents.MathTutorAgent.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AiAgents.MathTutorAgent.Infrastructure;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(MathTutorDbContext context)
    {
        if (await context.Topics.AnyAsync())
            return; // Already seeded

        // ========== TOPICS (NO EXPLICIT IDs!) ==========
        var topics = new List<Topic>
        {
            new() { Name = "Addition", Area = MathArea.Arithmetic, DifficultyBand = 1, Description = "Basic addition of integers" },
            new() { Name = "Subtraction", Area = MathArea.Arithmetic, DifficultyBand = 1, Description = "Basic subtraction" },
            new() { Name = "Multiplication", Area = MathArea.Arithmetic, DifficultyBand = 2, Description = "Multiplication tables and beyond" },
            new() { Name = "Division", Area = MathArea.Arithmetic, DifficultyBand = 2, Description = "Division and remainders" },
            new() { Name = "Fractions", Area = MathArea.Arithmetic, DifficultyBand = 3, Description = "Adding, subtracting, multiplying fractions" },
            new() { Name = "Algebraic Expressions", Area = MathArea.Algebra, DifficultyBand = 3, Description = "Simplifying expressions" },
            new() { Name = "Linear Equations", Area = MathArea.Algebra, DifficultyBand = 4, Description = "Solving linear equations" },
            new() { Name = "Set Theory", Area = MathArea.SetTheory, DifficultyBand = 3, Description = "Basic set operations" },
            new() { Name = "Propositional Logic", Area = MathArea.Logic, DifficultyBand = 4, Description = "Truth tables and tautologies" }
        };

        context.Topics.AddRange(topics);
        await context.SaveChangesAsync();

        // Now topics have IDs assigned by database
        var addition = topics[0];
        var subtraction = topics[1];
        var multiplication = topics[2];
        var division = topics[3];
        var fractions = topics[4];
        var expressions = topics[5];
        var equations = topics[6];
        var sets = topics[7];
        var logic = topics[8];

        // ========== TOPIC EDGES (DAG) ==========
        context.TopicEdges.AddRange(
            new TopicEdge { PrerequisiteTopicId = addition.Id, DependentTopicId = multiplication.Id },
            new TopicEdge { PrerequisiteTopicId = subtraction.Id, DependentTopicId = division.Id },
            new TopicEdge { PrerequisiteTopicId = multiplication.Id, DependentTopicId = fractions.Id },
            new TopicEdge { PrerequisiteTopicId = division.Id, DependentTopicId = fractions.Id },
            new TopicEdge { PrerequisiteTopicId = fractions.Id, DependentTopicId = expressions.Id },
            new TopicEdge { PrerequisiteTopicId = expressions.Id, DependentTopicId = equations.Id }
        );
        await context.SaveChangesAsync();

        // ========== QUESTIONS ==========
        context.Questions.AddRange(
            // Addition
            new Question { TopicId = addition.Id, Difficulty = 1, QuestionText = "What is 5 + 3?", CorrectAnswer = "8" },
            new Question { TopicId = addition.Id, Difficulty = 1, QuestionText = "What is 12 + 7?", CorrectAnswer = "19" },
            new Question { TopicId = addition.Id, Difficulty = 2, QuestionText = "What is 25 + 48?", CorrectAnswer = "73" },

            // Subtraction
            new Question { TopicId = subtraction.Id, Difficulty = 1, QuestionText = "What is 10 - 4?", CorrectAnswer = "6" },
            new Question { TopicId = subtraction.Id, Difficulty = 2, QuestionText = "What is 50 - 23?", CorrectAnswer = "27" },

            // Multiplication
            new Question { TopicId = multiplication.Id, Difficulty = 1, QuestionText = "What is 6 × 7?", CorrectAnswer = "42" },
            new Question { TopicId = multiplication.Id, Difficulty = 2, QuestionText = "What is 12 × 9?", CorrectAnswer = "108" },

            // Division
            new Question { TopicId = division.Id, Difficulty = 1, QuestionText = "What is 20 ÷ 4?", CorrectAnswer = "5" },
            new Question { TopicId = division.Id, Difficulty = 2, QuestionText = "What is 56 ÷ 8?", CorrectAnswer = "7" },

            // Fractions
            new Question { TopicId = fractions.Id, Difficulty = 3, QuestionText = "What is 1/2 + 1/4?", CorrectAnswer = "3/4" },
            new Question { TopicId = fractions.Id, Difficulty = 3, QuestionText = "What is 2/3 × 3/4?", CorrectAnswer = "1/2" },

            // Algebra
            new Question { TopicId = expressions.Id, Difficulty = 3, QuestionText = "Simplify: 3x + 2x", CorrectAnswer = "5x" },
            new Question { TopicId = equations.Id, Difficulty = 4, QuestionText = "Solve for x: 2x + 5 = 15", CorrectAnswer = "5" }
        );
        await context.SaveChangesAsync();

        // ========== KNOWLEDGE BASE (Optional demo data) ==========
        var documents = new List<KnowledgeDocument>
        {
            new() { Title = "Arithmetic Fundamentals", Author = "Math Department", FilePath = "/kb/arithmetic.pdf", UploadedAt = DateTime.UtcNow },
            new() { Title = "Algebra Basics", Author = "Math Department", FilePath = "/kb/algebra.pdf", UploadedAt = DateTime.UtcNow }
        };

        context.KnowledgeDocuments.AddRange(documents);
        await context.SaveChangesAsync();

        var doc1 = documents[0];
        var doc2 = documents[1];

        context.KnowledgeChunks.AddRange(
            new KnowledgeChunk
            {
                DocumentId = doc1.Id,
                PageNumber = 1,
                ChunkText = "Addition is the process of combining two or more numbers to get a sum.",
                Tags = new List<string> { "Addition", "Arithmetic" }
            },
            new KnowledgeChunk
            {
                DocumentId = doc1.Id,
                PageNumber = 2,
                ChunkText = "Subtraction is the process of finding the difference between two numbers.",
                Tags = new List<string> { "Subtraction", "Arithmetic" }
            },
            new KnowledgeChunk
            {
                DocumentId = doc2.Id,
                PageNumber = 1,
                ChunkText = "An algebraic expression contains variables, constants, and operators.",
                Tags = new List<string> { "Expressions", "Algebra" }
            }
        );
        await context.SaveChangesAsync();
    }
}