using System.Text.Json;
using System.Text.RegularExpressions;
using AiAgents.MathTutorAgent.Application.DTOs;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.SignalR.Client;

namespace AiAgents.MathTutorAgent.Web.Components.Pages;

public partial class Quiz
{
    [Parameter] public int StudentId { get; set; }

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
    private string? dragSource;
    private double TimeProgressPercent => timeLimitSeconds <= 0 ? 0 : Math.Max(0, remainingSeconds * 100.0 / timeLimitSeconds);

    protected override async Task OnInitializedAsync()
    {
        await InitializeSignalR();
        await GetNextQuestion();
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
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }

                questionStartTime = DateTime.UtcNow;
                timeLimitSeconds = Math.Max(30, currentQuestion?.TimeLimitSeconds ?? 120);
                remainingSeconds = timeLimitSeconds;
                SetupVisualPractice();
                StartCountdown();
                isLoading = false;
                await InvokeAsync(StateHasChanged);
                break;

            case TickOutcome.AnswerEvaluated:
                if (result.UiPayload is JsonElement feedbackJson)
                {
                    feedback = JsonSerializer.Deserialize<FeedbackDto>(
                        feedbackJson.GetRawText(),
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }

                isSubmitting = false;
                StopCountdown();
                await InvokeAsync(StateHasChanged);
                break;

            case TickOutcome.ExplanationReady:
                if (result.UiPayload is JsonElement explanationJson)
                {
                    explanation = JsonSerializer.Deserialize<ExplanationDto>(
                        explanationJson.GetRawText(),
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
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

        try
        {
            var response = await Http.PostAsJsonAsync("/api/agent/next-question", new { StudentId });
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
        if ((!timedOut && string.IsNullOrWhiteSpace(userAnswer)) || currentQuestion == null || isSubmitting)
        {
            return;
        }

        isSubmitting = true;
        var measuredMs = (int)(DateTime.UtcNow - questionStartTime).TotalMilliseconds;
        var timeMs = timedOut ? Math.Max(measuredMs, timeLimitSeconds * 1000) : measuredMs;
        var answerToSend = timedOut ? "__TIMEOUT__" : userAnswer.Trim();

        try
        {
            var response = await Http.PostAsJsonAsync("/api/agent/submit-answer", new
            {
                StudentId,
                QuestionId = currentQuestion.Id,
                Answer = answerToSend,
                TimeMs = timeMs,
                TimedOut = timedOut
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
                ErrorTag = (string?)null
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
        if (e.Key == "Enter" && !string.IsNullOrWhiteSpace(userAnswer) && !isSubmitting && !isLoading)
        {
            await SubmitAnswer(false);
        }
    }

    private void SetupVisualPractice()
    {
        visualPractice = null;

        if (currentQuestion == null || !currentQuestion.IsFirstQuestionInTopicForStudent)
        {
            return;
        }

        var match = Regex.Match(currentQuestion.QuestionText, @"(\d+)\s*([+\-])\s*(\d+)");

        if (!match.Success)
        {
            return;
        }

        var a = int.Parse(match.Groups[1].Value);
        var op = match.Groups[2].Value;
        var b = int.Parse(match.Groups[3].Value);

        if (op == "+")
        {
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
            visualPractice = new VisualPracticeModel
            {
                Mode = "subtraction",
                A = a,
                B = b,
                Remaining = a
            };
        }
    }

    private void BeginAppleDrag(string source)
    {
        dragSource = source;
    }

    private void BeginAppleDragA(DragEventArgs _) => BeginAppleDrag("A");
    private void BeginAppleDragB(DragEventArgs _) => BeginAppleDrag("B");
    private void BeginAppleDragMain(DragEventArgs _) => BeginAppleDrag("MAIN");

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
            return;
        }

        if (dragSource == "B" && visualPractice.SourceB > 0)
        {
            visualPractice.SourceB--;
            visualPractice.Dropped++;
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

        userAnswer = GetVisualResult().ToString();
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
            return "Hurry up!";
        }

        if (remainingSeconds <= timeLimitSeconds / 3)
        {
            return "Final stretch";
        }

        return "Steady pace";
    }

    public async ValueTask DisposeAsync()
    {
        StopCountdown();
        if (hubConnection != null)
        {
            await hubConnection.DisposeAsync();
        }
    }

    private class FeedbackDto
    {
        public bool IsCorrect { get; set; }
        public bool IsTimedOut { get; set; }
        public string CorrectAnswer { get; set; } = string.Empty;
        public double MasteryScore { get; set; }
        public string Decision { get; set; } = string.Empty;
        public int TimeLimitSeconds { get; set; }
        public double TimeSpentSeconds { get; set; }
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
