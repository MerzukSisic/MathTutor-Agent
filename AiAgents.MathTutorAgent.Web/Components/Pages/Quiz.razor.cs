using System.Text.Json;
using System.Text.RegularExpressions;
using System.Globalization;
using AiAgents.MathTutorAgent.Application.DTOs;
using AiAgents.MathTutorAgent.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.SignalR.Client;

namespace AiAgents.MathTutorAgent.Web.Components.Pages;

public partial class Quiz
{
    private static readonly JsonSerializerOptions CaseInsensitiveJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Parameter] public int StudentId { get; set; }
    private static readonly Regex ArithmeticOperationRegex = new(@"(\d+)\s*([+\-])\s*(\d+)", RegexOptions.Compiled);
    private static readonly Regex GeometryCountRegex = new(
        @"(?:how\s+many\s+(?<target>sides|vertices)\s+does\s+(?:an?\s+)?(?<shape>[a-z\-\s]+?)\s+have\??)|(?:koliko\s+(?<targetbs>stranica|vrhova)\s+ima\s+(?:[a-zčćžšđ]+\s+)?(?<shapebs>[a-zčćžšđ\-\s]+)\??)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private QuestionDto? currentQuestion;
    private string userAnswer = string.Empty;
    private FeedbackDto? feedback;
    private ExplanationDto? explanation;
    private bool isLoading;
    private bool isSubmitting;
    private HubConnection? hubConnection;
    private DateTime questionStartTime;
    private int timeLimitSeconds = 120;
    private int remainingSeconds = 120;
    private Timer? countdownTimer;
    private VisualPracticeModel? visualPractice;
    private GeometryInteractionModel? geometryInteraction;
    private CrossMathChallengeModel? milestoneChallenge;
    private string? dragSource;
    private double TimeProgressPercent => timeLimitSeconds <= 0 ? 0 : Math.Max(0, remainingSeconds * 100.0 / timeLimitSeconds);
    private string L(string bs, string en) => UiPrefs.Language == UiLanguage.Bs ? bs : en;
    private string GetQuestionTextForDisplay(string? rawText) =>
        ContentLocalization.LocalizeQuestionText(rawText, UiPrefs.LanguageCode);

    protected override async Task OnInitializedAsync()
    {
        UiPrefs.Changed += HandleLanguageChanged;
        await InitializeSignalR();
        await GetNextQuestion();
    }

    private void HandleLanguageChanged()
    {
        _ = InvokeAsync(async () =>
        {
            if (currentQuestion != null && explanation != null && !isLoading && !isSubmitting)
            {
                await RequestExplanation();
            }

            StateHasChanged();
        });
    }

    private async Task InitializeSignalR()
    {
        hubConnection = new HubConnectionBuilder()
            .WithUrl(Navigation.ToAbsoluteUri($"/agenthub?studentId={StudentId}"))
            .Build();

        hubConnection.On<MathTickResult>("AgentTick", async result =>
        {
            if (result.StudentId == StudentId)
            {
                await HandleAgentTick(result);
            }
        });

        await hubConnection.StartAsync();
    }

    private async Task HandleAgentTick(MathTickResult result)
    {
        switch (result.Outcome)
        {
            case TickOutcome.QuestionGenerated:
                if (result.UiPayload is JsonElement questionJson)
                {
                    currentQuestion = JsonSerializer.Deserialize<QuestionDto>(
                        questionJson.GetRawText(),
                        CaseInsensitiveJsonOptions);
                }

                questionStartTime = DateTime.UtcNow;
                timeLimitSeconds = Math.Max(30, currentQuestion?.TimeLimitSeconds ?? 120);
                remainingSeconds = timeLimitSeconds;
                SetupQuestionInteractions();
                StartCountdown();
                isLoading = false;
                await InvokeAsync(StateHasChanged);
                break;

            case TickOutcome.AnswerEvaluated:
                if (result.UiPayload is JsonElement feedbackJson)
                {
                    feedback = JsonSerializer.Deserialize<FeedbackDto>(
                        feedbackJson.GetRawText(),
                        CaseInsensitiveJsonOptions);
                }

                isSubmitting = false;
                StopCountdown();
                milestoneChallenge = ToChallengeModel(feedback?.MilestoneChallenge);
                await InvokeAsync(StateHasChanged);
                break;

            case TickOutcome.ExplanationReady:
                if (result.UiPayload is JsonElement explanationJson)
                {
                    explanation = JsonSerializer.Deserialize<ExplanationDto>(
                        explanationJson.GetRawText(),
                        CaseInsensitiveJsonOptions);
                }

                await InvokeAsync(StateHasChanged);
                break;
        }
    }

    private async Task GetNextQuestion()
    {
        StopCountdown();
        isLoading = true;
        feedback = null;
        explanation = null;
        userAnswer = string.Empty;
        visualPractice = null;
        geometryInteraction = null;
        milestoneChallenge = null;

        try
        {
            var response = await Http.PostAsJsonAsync("/api/agent/next_question", new
            {
                StudentId,
                Language = UiPrefs.LanguageCode
            });
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            isLoading = false;
        }
    }

    private async Task SubmitAnswer(bool timedOut = false)
    {
        if ((!timedOut && string.IsNullOrWhiteSpace(userAnswer)) || currentQuestion == null || isSubmitting || feedback != null)
        {
            return;
        }

        isSubmitting = true;
        var measuredMs = (int)(DateTime.UtcNow - questionStartTime).TotalMilliseconds;
        var timeMs = timedOut ? Math.Max(measuredMs, timeLimitSeconds * 1000) : measuredMs;
        var answerToSend = timedOut ? "__TIMEOUT__" : userAnswer.Trim();

        try
        {
            var response = await Http.PostAsJsonAsync("/api/agent/submit_answer", new
            {
                StudentId,
                QuestionId = currentQuestion.Id,
                Answer = answerToSend,
                TimeMs = timeMs,
                TimedOut = timedOut,
                Language = UiPrefs.LanguageCode
            });
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            isSubmitting = false;
        }
    }

    private async Task RequestExplanation()
    {
        if (currentQuestion == null)
        {
            return;
        }

        try
        {
            var response = await Http.PostAsJsonAsync("/api/agent/explain", new
            {
                StudentId,
                QuestionId = currentQuestion.Id,
                TopicId = currentQuestion.TopicId,
                ErrorTag = (string?)null,
                Language = UiPrefs.LanguageCode
            });
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private async Task HandleKeyPress(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && !string.IsNullOrWhiteSpace(userAnswer) && !isSubmitting && !isLoading && feedback == null)
        {
            await SubmitAnswer(false);
        }
    }

    private void SetupQuestionInteractions()
    {
        SetupVisualPractice();
        SetupGeometryInteraction();
    }

    private void SetupVisualPractice()
    {
        visualPractice = null;

        if (currentQuestion == null || !currentQuestion.IsFirstQuestionInTopicForStudent)
        {
            return;
        }

        var match = ArithmeticOperationRegex.Match(currentQuestion.QuestionText);

        if (!match.Success)
        {
            return;
        }

        var a = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        var op = match.Groups[2].Value;
        var b = int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);

        if (op == "+")
        {
            var result = a + b;
            if (a > 10 || b > 10 || result > 20)
            {
                return;
            }

            visualPractice = new VisualPracticeModel
            {
                Mode = "addition",
                A = a,
                B = b,
                SourceA = a,
                SourceB = b
            };
            return;
        }

        if (op == "-" && a >= b)
        {
            var result = a - b;
            if (a > 20 || b > 10 || result > 12)
            {
                return;
            }

            visualPractice = new VisualPracticeModel
            {
                Mode = "subtraction",
                A = a,
                B = b,
                Remaining = a
            };
        }
    }

    private void SetupGeometryInteraction()
    {
        geometryInteraction = null;

        if (currentQuestion == null)
        {
            return;
        }

        var match = GeometryCountRegex.Match(currentQuestion.QuestionText);
        if (!match.Success)
        {
            return;
        }

        var target = match.Groups["target"].Success
            ? match.Groups["target"].Value.Trim().ToLowerInvariant()
            : match.Groups["targetbs"].Value.Trim().ToLowerInvariant() switch
            {
                "stranica" => "sides",
                "vrhova" => "vertices",
                _ => string.Empty
            };
        var shapeLabel = match.Groups["shape"].Success
            ? match.Groups["shape"].Value.Trim().ToLowerInvariant()
            : match.Groups["shapebs"].Value.Trim().ToLowerInvariant();
        var shapeKey = NormalizeShapeKey(shapeLabel);
        if (shapeKey == null)
        {
            return;
        }

        var elementCount = ResolveElementCount(shapeKey, target);
        if ((elementCount < 3 || elementCount > 10) && shapeKey != "cube")
        {
            return;
        }

        geometryInteraction = new GeometryInteractionModel
        {
            ShapeKey = shapeKey,
            ShapeLabel = ToTitleCase(shapeLabel),
            CountTarget = target,
            ElementCount = elementCount,
            QuestionText = currentQuestion.QuestionText
        };
    }

    private static string? NormalizeShapeKey(string rawShape)
    {
        if (string.IsNullOrWhiteSpace(rawShape))
        {
            return null;
        }

        var normalized = rawShape.Trim().ToLowerInvariant();

        if (normalized.Contains("triangle")) return "triangle";
        if (normalized.Contains("trokut") || normalized.Contains("trougao")) return "triangle";
        if (normalized.Contains("square")) return "square";
        if (normalized.Contains("kvadrat")) return "square";
        if (normalized.Contains("rectangle")) return "rectangle";
        if (normalized.Contains("pravougaonik") || normalized.Contains("pravokutnik")) return "rectangle";
        if (normalized.Contains("quadrilateral")) return "quadrilateral";
        if (normalized.Contains("četverougao") || normalized.Contains("cetverougao") || normalized.Contains("četverokut") || normalized.Contains("cetverokut")) return "quadrilateral";
        if (normalized.Contains("pentagon")) return "pentagon";
        if (normalized.Contains("petougao") || normalized.Contains("petokut")) return "pentagon";
        if (normalized.Contains("hexagon")) return "hexagon";
        if (normalized.Contains("šesterougao") || normalized.Contains("sesterougao") || normalized.Contains("šesterokut") || normalized.Contains("sesterokut")) return "hexagon";
        if (normalized.Contains("heptagon")) return "heptagon";
        if (normalized.Contains("sedmerougao") || normalized.Contains("sedmerokut")) return "heptagon";
        if (normalized.Contains("octagon")) return "octagon";
        if (normalized.Contains("osmerougao") || normalized.Contains("osmerokut")) return "octagon";
        if (normalized.Contains("nonagon")) return "nonagon";
        if (normalized.Contains("deveterougao") || normalized.Contains("deveterokut")) return "nonagon";
        if (normalized.Contains("decagon")) return "decagon";
        if (normalized.Contains("deseterougao") || normalized.Contains("deseterokut")) return "decagon";
        if (normalized.Contains("cube")) return "cube";
        if (normalized.Contains("kocka")) return "cube";

        return null;
    }

    private static int ResolveElementCount(string shapeKey, string target)
    {
        var isSides = string.Equals(target, "sides", StringComparison.OrdinalIgnoreCase);

        return shapeKey switch
        {
            "triangle" => 3,
            "square" => 4,
            "rectangle" => 4,
            "quadrilateral" => 4,
            "pentagon" => 5,
            "hexagon" => 6,
            "heptagon" => 7,
            "octagon" => 8,
            "nonagon" => 9,
            "decagon" => 10,
            "cube" when isSides => 6,
            "cube" => 8,
            _ => 0
        };
    }

    private static string ToTitleCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return string.Join(" ", value.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(token => char.ToUpperInvariant(token[0]) + token[1..].ToLowerInvariant()));
    }

    private void HandleGeometrySelectionChanged(int selectedCount)
    {
        userAnswer = selectedCount <= 0
            ? string.Empty
            : selectedCount.ToString(CultureInfo.InvariantCulture);
    }

    private async Task HandleMilestoneChallengeCompleted()
    {
        if (milestoneChallenge == null)
        {
            return;
        }

        try
        {
            var currentKey = milestoneChallenge.ChallengeKey;
            var response = await Http.PostAsJsonAsync("/api/agent/complete_milestone", new
            {
                StudentId,
                ChallengeKey = currentKey,
                Language = UiPrefs.LanguageCode
            });
            response.EnsureSuccessStatusCode();

            var payload = await response.Content.ReadFromJsonAsync<MilestoneCompletionResponse>();
            milestoneChallenge = ToChallengeModel(payload?.NextMilestone);

            if (milestoneChallenge != null)
            {
                await InvokeAsync(StateHasChanged);
                return;
            }

            await GetNextQuestion();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private static CrossMathChallengeModel? ToChallengeModel(CrossMathMilestoneDto? dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.ChallengeKey))
        {
            return null;
        }

        var mode = dto.Mode?.Trim().ToLowerInvariant() switch
        {
            "addition" => CrossMathOperationMode.Addition,
            "subtraction" => CrossMathOperationMode.Subtraction,
            "multiplication" => CrossMathOperationMode.Multiplication,
            "division" => CrossMathOperationMode.Division,
            "mixed" => CrossMathOperationMode.Mixed,
            _ => CrossMathOperationMode.Addition
        };

        return new CrossMathChallengeModel
        {
            ChallengeKey = dto.ChallengeKey,
            Title = dto.Title,
            Subtitle = dto.Subtitle,
            Mode = mode,
            Size = dto.Size
        };
    }

    private void BeginAppleDrag(string source)
    {
        dragSource = source;
    }

    private void BeginAppleDragA(DragEventArgs _) => BeginAppleDrag("A");
    private void BeginAppleDragB(DragEventArgs _) => BeginAppleDrag("B");
    private void BeginAppleDragMain(DragEventArgs _) => BeginAppleDrag("MAIN");

    private void TakeAppleFromA()
    {
        if (visualPractice == null || visualPractice.Mode != "addition" || visualPractice.SourceA <= 0)
        {
            return;
        }

        visualPractice.SourceA--;
        visualPractice.Dropped++;
    }

    private void TakeAppleFromB()
    {
        if (visualPractice == null || visualPractice.Mode != "addition" || visualPractice.SourceB <= 0)
        {
            return;
        }

        visualPractice.SourceB--;
        visualPractice.Dropped++;
    }

    private void TakeAppleFromMain()
    {
        if (visualPractice == null || visualPractice.Mode != "subtraction")
        {
            return;
        }

        if (visualPractice.Remaining <= 0 || visualPractice.Removed >= visualPractice.B)
        {
            return;
        }

        visualPractice.Remaining--;
        visualPractice.Removed++;
    }

    private void DropAppleToAnswer(DragEventArgs _)
    {
        if (visualPractice == null || visualPractice.Mode != "addition")
        {
            return;
        }

        if (dragSource == "A" && visualPractice.SourceA > 0)
        {
            visualPractice.SourceA--;
            visualPractice.Dropped++;
            dragSource = null;
            return;
        }

        if (dragSource == "B" && visualPractice.SourceB > 0)
        {
            visualPractice.SourceB--;
            visualPractice.Dropped++;
            dragSource = null;
        }
    }

    private void DropAppleToRemove(DragEventArgs _)
    {
        if (visualPractice == null || visualPractice.Mode != "subtraction")
        {
            return;
        }

        if (dragSource == "MAIN" && visualPractice.Remaining > 0 && visualPractice.Removed < visualPractice.B)
        {
            visualPractice.Remaining--;
            visualPractice.Removed++;
            dragSource = null;
        }
    }

    private int GetVisualResult()
    {
        if (visualPractice == null)
        {
            return 0;
        }

        return visualPractice.Mode == "addition" ? visualPractice.Dropped : visualPractice.Remaining;
    }

    private void ApplyVisualResult()
    {
        if (visualPractice == null)
        {
            return;
        }

        userAnswer = GetVisualResult().ToString(CultureInfo.InvariantCulture);
    }

    private void StartCountdown()
    {
        StopCountdown();
        countdownTimer = new Timer(async _ =>
        {
            if (isSubmitting || isLoading || currentQuestion == null || feedback != null)
            {
                return;
            }

            remainingSeconds = Math.Max(0, remainingSeconds - 1);
            await InvokeAsync(StateHasChanged);

            if (remainingSeconds != 0)
            {
                return;
            }

            StopCountdown();
            await InvokeAsync(async () =>
            {
                if (!isSubmitting && feedback == null)
                {
                    await SubmitAnswer(true);
                }
            });
        }, null, 1000, 1000);
    }

    private void StopCountdown()
    {
        countdownTimer?.Dispose();
        countdownTimer = null;
    }

    private void GoBack()
    {
        Navigation.NavigateTo("/");
    }

    private string GetTimerHint()
    {
        if (remainingSeconds <= 10)
        {
            return L("Požuri!", "Hurry up!");
        }

        if (remainingSeconds <= timeLimitSeconds / 3)
        {
            return L("Završnica", "Final stretch");
        }

        return L("Samo nastavi", "Steady pace");
    }

    public async ValueTask DisposeAsync()
    {
        UiPrefs.Changed -= HandleLanguageChanged;
        StopCountdown();
        if (hubConnection != null)
        {
            await hubConnection.DisposeAsync();
        }

        GC.SuppressFinalize(this);
    }

    private sealed class FeedbackDto
    {
        public bool IsCorrect { get; set; }
        public bool IsTimedOut { get; set; }
        public string CorrectAnswer { get; set; } = string.Empty;
        public double MasteryScore { get; set; }
        public string Decision { get; set; } = string.Empty;
        public int TimeLimitSeconds { get; set; }
        public double TimeSpentSeconds { get; set; }
        public CrossMathMilestoneDto? MilestoneChallenge { get; set; }
    }

    private sealed class MilestoneCompletionResponse
    {
        public CrossMathMilestoneDto? NextMilestone { get; set; }
    }

    private sealed class VisualPracticeModel
    {
        public string Mode { get; set; } = string.Empty;
        public int A { get; set; }
        public int B { get; set; }
        public int SourceA { get; set; }
        public int SourceB { get; set; }
        public int Dropped { get; set; }
        public int Removed { get; set; }
        public int Remaining { get; set; }
    }
}
