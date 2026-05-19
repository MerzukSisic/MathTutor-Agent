using AiAgents.MathTutorAgent.Domain.Entities;
using Bogus;
using Microsoft.EntityFrameworkCore;

namespace AiAgents.MathTutorAgent.Infrastructure;

public static class BogusTrainingDataSeeder
{
    private const string BogusEmailPrefix = "bogus.synthetic.";
    private const string OldSyntheticEmailPrefix = "ml.synthetic.";

    public static async Task SeedAsync(MathTutorDbContext context, CancellationToken ct = default)
    {
        if (!GetBoolEnv("MATH_TUTOR_ENABLE_BOGUS_DATASET", defaultValue: false))
        {
            return;
        }

        var recreate = GetBoolEnv("MATH_TUTOR_BOGUS_RECREATE", defaultValue: false);
        var studentsToGenerate = GetIntEnv("MATH_TUTOR_BOGUS_STUDENTS", 40, 30, 50);
        var minAttemptsPerStudent = GetIntEnv("MATH_TUTOR_BOGUS_MIN_ATTEMPTS", 180, 80, 1200);
        var maxAttemptsPerStudent = GetIntEnv("MATH_TUTOR_BOGUS_MAX_ATTEMPTS", 260, minAttemptsPerStudent, 2000);

        var questions = await context.Questions
            .AsNoTracking()
            .Select(q => new QuestionSeedRow(
                q.Id,
                q.TopicId,
                q.Difficulty,
                q.CorrectAnswer,
                q.CommonMistakes))
            .ToListAsync(ct);

        if (questions.Count == 0)
        {
            return;
        }

        minAttemptsPerStudent = Math.Max(minAttemptsPerStudent, questions.Count);
        maxAttemptsPerStudent = Math.Max(maxAttemptsPerStudent, minAttemptsPerStudent + 10);

        if (recreate)
        {
            await DeleteSyntheticStudentsAsync(context, ct);
        }

        var existingBogusCount = await context.Students
            .CountAsync(s => EF.Functions.Like(s.Email, $"{BogusEmailPrefix}%@example.local"), ct);

        if (!recreate && existingBogusCount >= studentsToGenerate)
        {
            return;
        }

        var studentsNeeded = recreate
            ? studentsToGenerate
            : Math.Max(0, studentsToGenerate - existingBogusCount);

        if (studentsNeeded == 0)
        {
            return;
        }

        Randomizer.Seed = new Random(20260519);
        var faker = new Faker("en");
        var students = new List<Student>(studentsNeeded);

        for (var i = 0; i < studentsNeeded; i++)
        {
            var syntheticIndex = existingBogusCount + i + 1;
            students.Add(new Student
            {
                Name = faker.Name.FullName(),
                Email = $"{BogusEmailPrefix}{syntheticIndex}@example.local",
                CreatedAt = faker.Date.Between(DateTime.UtcNow.AddDays(-365), DateTime.UtcNow.AddDays(-35))
            });
        }

        context.Students.AddRange(students);
        await context.SaveChangesAsync(ct);

        var attempts = new List<Attempt>(studentsNeeded * ((minAttemptsPerStudent + maxAttemptsPerStudent) / 2));
        var topicStates = new List<StudentTopicState>(studentsNeeded * 20);
        var targetAccuracies = BuildTargetAccuracies(studentsNeeded, faker);

        for (var studentIndex = 0; studentIndex < students.Count; studentIndex++)
        {
            var student = students[studentIndex];
            var attemptCount = faker.Random.Int(minAttemptsPerStudent, maxAttemptsPerStudent);
            var accuracyTarget = targetAccuracies[studentIndex];
            var correctTarget = Math.Clamp((int)Math.Round(attemptCount * accuracyTarget), 1, attemptCount);
            var correctnessMask = BuildCorrectnessMask(attemptCount, correctTarget, faker);

            var questionPlan = BuildQuestionPlan(questions, attemptCount, faker);
            var studentAttempts = new List<Attempt>(attemptCount);
            var timeline = student.CreatedAt.AddMinutes(faker.Random.Int(15, 180));

            for (var i = 0; i < attemptCount; i++)
            {
                var question = questionPlan[i];
                var isCorrect = correctnessMask[i];
                var timeMs = BuildTimeMs(question.Difficulty, isCorrect, faker);
                var submittedAnswer = BuildAnswer(question, isCorrect, faker);

                timeline = timeline.AddSeconds(faker.Random.Int(45, 360));
                if (timeline > DateTime.UtcNow.AddMinutes(-1))
                {
                    timeline = DateTime.UtcNow.AddMinutes(-faker.Random.Int(2, 120));
                }

                var attempt = new Attempt
                {
                    StudentId = student.Id,
                    QuestionId = question.Id,
                    IsCorrect = isCorrect,
                    TimeMs = timeMs,
                    AnswerRaw = submittedAnswer,
                    ErrorTagsDetected = BuildErrorTags(isCorrect, timeMs, faker),
                    CreatedAt = timeline
                };

                studentAttempts.Add(attempt);
            }

            attempts.AddRange(studentAttempts);
            topicStates.AddRange(BuildTopicStates(student.Id, studentAttempts, questionPlan, faker));
        }

        context.Attempts.AddRange(attempts);
        context.StudentTopicStates.AddRange(topicStates);
        await context.SaveChangesAsync(ct);
    }

    private static async Task DeleteSyntheticStudentsAsync(MathTutorDbContext context, CancellationToken ct)
    {
        var syntheticIds = await context.Students
            .Where(s => EF.Functions.Like(s.Email, $"{BogusEmailPrefix}%@example.local")
                     || EF.Functions.Like(s.Email, $"{OldSyntheticEmailPrefix}%@example.local"))
            .Select(s => s.Id)
            .ToListAsync(ct);

        if (syntheticIds.Count == 0)
        {
            return;
        }

        var syntheticSet = syntheticIds.ToHashSet();

        var attempts = await context.Attempts.Where(a => syntheticSet.Contains(a.StudentId)).ToListAsync(ct);
        var states = await context.StudentTopicStates.Where(s => syntheticSet.Contains(s.StudentId)).ToListAsync(ct);
        var schedule = await context.RevisionScheduleItems.Where(s => syntheticSet.Contains(s.StudentId)).ToListAsync(ct);
        var workItems = await context.WorkItems.Where(w => syntheticSet.Contains(w.StudentId)).ToListAsync(ct);
        var notes = await context.ImageNotes.Where(n => syntheticSet.Contains(n.StudentId)).ToListAsync(ct);
        var challenges = await context.StudentChallengeProgress.Where(c => syntheticSet.Contains(c.StudentId)).ToListAsync(ct);
        var accounts = await context.UserAccounts.Where(a => a.StudentId != null && syntheticSet.Contains(a.StudentId.Value)).ToListAsync(ct);
        var students = await context.Students.Where(s => syntheticSet.Contains(s.Id)).ToListAsync(ct);

        context.Attempts.RemoveRange(attempts);
        context.StudentTopicStates.RemoveRange(states);
        context.RevisionScheduleItems.RemoveRange(schedule);
        context.WorkItems.RemoveRange(workItems);
        context.ImageNotes.RemoveRange(notes);
        context.StudentChallengeProgress.RemoveRange(challenges);
        context.UserAccounts.RemoveRange(accounts);
        context.Students.RemoveRange(students);
        await context.SaveChangesAsync(ct);
    }

    private static List<QuestionSeedRow> BuildQuestionPlan(IReadOnlyList<QuestionSeedRow> questions, int attemptCount, Faker faker)
    {
        var plan = new List<QuestionSeedRow>(attemptCount);
        var shuffled = questions.OrderBy(_ => faker.Random.Int()).ToList();
        var initialCoverage = Math.Min(attemptCount, shuffled.Count);
        plan.AddRange(shuffled.Take(initialCoverage));

        for (var i = initialCoverage; i < attemptCount; i++)
        {
            plan.Add(questions[faker.Random.Int(0, questions.Count - 1)]);
        }

        return plan;
    }

    private static bool[] BuildCorrectnessMask(int attemptCount, int correctTarget, Faker faker)
    {
        var mask = Enumerable.Repeat(false, attemptCount).ToArray();
        var indexes = Enumerable.Range(0, attemptCount).OrderBy(_ => faker.Random.Int()).Take(correctTarget);

        foreach (var index in indexes)
        {
            mask[index] = true;
        }

        return mask;
    }

    private static int BuildTimeMs(int difficulty, bool isCorrect, Faker faker)
    {
        var min = 2500 + (difficulty * 1400);
        var max = 9000 + (difficulty * 3400);
        if (!isCorrect)
        {
            min += 1200;
            max += 4200;
        }

        return faker.Random.Int(min, max);
    }

    private static string BuildAnswer(QuestionSeedRow question, bool isCorrect, Faker faker)
    {
        if (isCorrect)
        {
            return question.CorrectAnswer;
        }

        if (question.CommonMistakes.Count > 0)
        {
            var mistaken = faker.Random.ListItem(question.CommonMistakes);
            if (!string.Equals(mistaken.Trim(), question.CorrectAnswer.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return mistaken;
            }
        }

        if (double.TryParse(question.CorrectAnswer, out var numeric))
        {
            var delta = faker.Random.Int(1, 4);
            var sign = faker.Random.Bool() ? 1 : -1;
            var mutated = numeric + (delta * sign);
            return Math.Abs(mutated % 1) < 0.0001
                ? ((int)Math.Round(mutated)).ToString()
                : mutated.ToString("0.##");
        }

        return question.CorrectAnswer.Equals("yes", StringComparison.OrdinalIgnoreCase) ? "No"
            : question.CorrectAnswer.Equals("no", StringComparison.OrdinalIgnoreCase) ? "Yes"
            : $"{question.CorrectAnswer} ?";
    }

    private static List<double> BuildTargetAccuracies(int studentsNeeded, Faker faker)
    {
        var targets = new List<double>(studentsNeeded);
        if (studentsNeeded == 40)
        {
            // Requested explicit cohort distribution:
            // 10 students: 10-60%, 10 students: 61-70%, 15 students: 71-85%, 5 students: 86-100%.
            AddRange(targets, 10, 0.10, 0.60, faker);
            AddRange(targets, 10, 0.61, 0.70, faker);
            AddRange(targets, 15, 0.71, 0.85, faker);
            AddRange(targets, 5, 0.86, 1.00, faker);
            return targets.OrderBy(_ => faker.Random.Double()).ToList();
        }

        // Fallback for non-40 generation counts (keeps broad spread).
        var g1 = (int)Math.Round(studentsNeeded * 0.25, MidpointRounding.AwayFromZero);
        var g2 = (int)Math.Round(studentsNeeded * 0.25, MidpointRounding.AwayFromZero);
        var g4 = Math.Max(1, (int)Math.Round(studentsNeeded * 0.125, MidpointRounding.AwayFromZero));
        var g3 = Math.Max(0, studentsNeeded - g1 - g2 - g4);
        AddRange(targets, g1, 0.10, 0.60, faker);
        AddRange(targets, g2, 0.10, 0.70, faker);
        AddRange(targets, g3, 0.15, 0.85, faker);
        AddRange(targets, g4, 0.05, 1.00, faker);
        return targets.OrderBy(_ => faker.Random.Double()).Take(studentsNeeded).ToList();
    }

    private static void AddRange(List<double> targets, int count, double min, double max, Faker faker)
    {
        for (var i = 0; i < count; i++)
        {
            targets.Add(faker.Random.Double(min, max));
        }
    }

    private static List<string> BuildErrorTags(bool isCorrect, int timeMs, Faker faker)
    {
        if (isCorrect)
        {
            var tags = new List<string>();
            if (timeMs < 5000)
            {
                tags.Add("fast_response");
            }

            if (faker.Random.Double() < 0.08)
            {
                tags.Add("careful_reasoning");
            }

            return tags;
        }

        var errorTags = new List<string> { faker.Random.ArrayElement(["wrong_concept", "calculation_error", "symbol_confusion"]) };
        if (timeMs > 14000)
        {
            errorTags.Add("slow_response");
        }

        return errorTags;
    }

    private static IEnumerable<StudentTopicState> BuildTopicStates(
        int studentId,
        IReadOnlyList<Attempt> studentAttempts,
        IReadOnlyList<QuestionSeedRow> plan,
        Faker faker)
    {
        var grouped = studentAttempts
            .Zip(plan, (attempt, question) => new { attempt, question.TopicId })
            .GroupBy(x => x.TopicId);

        foreach (var group in grouped)
        {
            var attempts = group.Select(x => x.attempt).ToList();
            var attemptCount = attempts.Count;
            var accuracy = attemptCount == 0 ? 0 : attempts.Count(a => a.IsCorrect) / (double)attemptCount;
            var avgTimeSeconds = attemptCount == 0 ? 0 : attempts.Average(a => a.TimeMs) / 1000.0;

            var mastery = (float)Math.Clamp(45 + accuracy * 55 - Math.Max(0, avgTimeSeconds - 8) * 1.2, 35, 98);
            var confidence = Math.Clamp(accuracy * 0.85 + Math.Min(0.15, attemptCount / 120.0), 0.2, 0.99);
            var forgettingRisk = Math.Clamp(0.75 - confidence * 0.55 + faker.Random.Double(-0.05, 0.05), 0.02, 0.9);

            yield return new StudentTopicState
            {
                StudentId = studentId,
                TopicId = group.Key,
                MasteryScore = mastery,
                Confidence = confidence,
                ForgettingRisk = forgettingRisk,
                LastPracticedUtc = attempts.Max(a => a.CreatedAt)
            };
        }
    }

    private static bool GetBoolEnv(string key, bool defaultValue)
    {
        var value = Environment.GetEnvironmentVariable(key);
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "1" => true,
            "true" => true,
            "yes" => true,
            "on" => true,
            "0" => false,
            "false" => false,
            "no" => false,
            "off" => false,
            _ => defaultValue
        };
    }

    private static int GetIntEnv(string key, int defaultValue, int min, int max)
    {
        var value = Environment.GetEnvironmentVariable(key);
        if (!int.TryParse(value, out var parsed))
        {
            parsed = defaultValue;
        }

        return Math.Clamp(parsed, min, max);
    }

    private sealed record QuestionSeedRow(
        int Id,
        int TopicId,
        int Difficulty,
        string CorrectAnswer,
        List<string> CommonMistakes);
}
