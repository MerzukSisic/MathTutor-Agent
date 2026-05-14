using System.Text.Json;
using AiAgents.MathTutorAgent.Application.DTOs;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using System.Net.Http.Json;

namespace AiAgents.MathTutorAgent.Web.Components.Pages;

public partial class Admin
{
    private PerformanceMetricsDto? metrics;
    private List<AdminQuestionDto>? questions;
    private List<StudentDto>? students;
    private string? flashMessage;
    private bool flashIsError;

    protected override async Task OnInitializedAsync()
    {
        ApplyStatusFromQuery();
        await LoadMetrics();
        await LoadQuestions();
        await LoadStudents();
    }

    private async Task LoadMetrics()
    {
        try
        {
            metrics = await Http.GetFromJsonAsync<PerformanceMetricsDto>("/api/admin/performance-metrics");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading metrics: {ex.Message}");
        }
    }

    private async Task LoadQuestions()
    {
        try
        {
            questions = await Http.GetFromJsonAsync<List<AdminQuestionDto>>("/api/admin/questions");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading questions: {ex.Message}");
        }
    }

    private async Task LoadStudents()
    {
        try
        {
            students = await Http.GetFromJsonAsync<List<StudentDto>>("/api/admin/students");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading students: {ex.Message}");
        }
    }

    private void OpenAddStudentDialog()
    {
        Navigation.NavigateTo("/admin/add-student");
    }

    private Task OpenAddQuestionDialog()
    {
        Navigation.NavigateTo("/admin/add-question");
        return Task.CompletedTask;
    }

    private void EditQuestion(AdminQuestionDto question)
    {
        Navigation.NavigateTo($"/admin/edit-question/{question.Id}");
    }

    private async Task DeleteQuestion(int id)
    {
        var confirmed = await JS.InvokeAsync<bool>("confirm", new object?[] { "Delete this question permanently?" });
        if (!confirmed)
        {
            return;
        }

        var response = await Http.DeleteAsync($"/api/admin/questions/{id}");
        if (response.IsSuccessStatusCode)
        {
            await LoadQuestions();
            flashMessage = "Question deleted successfully.";
            flashIsError = false;
        }
        else
        {
            flashMessage = $"Delete failed ({response.StatusCode}).";
            flashIsError = true;
        }
    }

    private void ViewProfile(int studentId)
    {
        Navigation.NavigateTo($"/profile/{studentId}");
    }

    private void EditStudent(int studentId)
    {
        Navigation.NavigateTo($"/admin/edit-student/{studentId}");
    }

    private async Task DeleteStudent(int studentId, string studentName)
    {
        var confirmed = await JS.InvokeAsync<bool>("confirm", new object?[] { $"Delete student '{studentName}'?" });
        if (!confirmed)
        {
            return;
        }

        var response = await Http.DeleteAsync($"/api/admin/students/{studentId}");
        if (response.IsSuccessStatusCode)
        {
            await LoadStudents();
            await LoadMetrics();
            flashMessage = "Student deleted successfully.";
            flashIsError = false;
            return;
        }

        var errorRaw = await response.Content.ReadAsStringAsync();
        flashMessage = string.IsNullOrWhiteSpace(errorRaw)
            ? $"Student delete failed ({response.StatusCode})."
            : ExtractApiErrorMessage(errorRaw);
        flashIsError = true;
    }

    private void ApplyStatusFromQuery()
    {
        var uri = Navigation.ToAbsoluteUri(Navigation.Uri);
        var query = QueryHelpers.ParseQuery(uri.Query);
        if (!query.TryGetValue("status", out var statusValue))
        {
            return;
        }

        switch (statusValue.ToString())
        {
            case "student-created":
                flashMessage = "Student created successfully.";
                flashIsError = false;
                break;
            case "question-created":
                flashMessage = "Question created successfully.";
                flashIsError = false;
                break;
            case "question-updated":
                flashMessage = "Question updated successfully.";
                flashIsError = false;
                break;
            case "student-updated":
                flashMessage = "Student updated successfully.";
                flashIsError = false;
                break;
        }

        Navigation.NavigateTo("/admin", replace: true);
    }

    private string GetCompletionRatePercent()
    {
        if (metrics == null || metrics.TotalWorkItems <= 0)
        {
            return "0%";
        }

        var value = metrics.CompletedWorkItems * 100.0 / metrics.TotalWorkItems;
        return $"{Math.Clamp(value, 0, 100):F1}%";
    }

    private static string ExtractApiErrorMessage(string raw)
    {
        try
        {
            using var doc = JsonDocument.Parse(raw);
            if (doc.RootElement.TryGetProperty("message", out var msgElement) &&
                msgElement.ValueKind == JsonValueKind.String)
            {
                return msgElement.GetString() ?? raw;
            }
        }
        catch
        {
            // ignore parse errors
        }

        return raw;
    }
}
