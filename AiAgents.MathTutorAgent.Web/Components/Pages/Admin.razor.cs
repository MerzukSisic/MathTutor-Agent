using System.Text.Json;
using AiAgents.MathTutorAgent.Application.DTOs;
using AiAgents.MathTutorAgent.Web.Services;
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
    private string L(string bs, string en) => UiPrefs.Language == UiLanguage.Bs ? bs : en;

    protected override async Task OnInitializedAsync()
    {
        UiPrefs.Changed += HandleLanguageChanged;
        ApplyStatusFromQuery();
        await LoadMetrics();
        await LoadQuestions();
        await LoadStudents();
    }

    private async Task LoadMetrics()
    {
        try
        {
            metrics = await Http.GetFromJsonAsync<PerformanceMetricsDto>("/api/admin/performance_metrics");
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
            questions = await Http.GetFromJsonAsync<List<AdminQuestionDto>>($"/api/admin/questions?lang={UiPrefs.LanguageCode}");
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
        var confirmed = await JS.InvokeAsync<bool>("confirm", new object?[] { L("Trajno obrisati ovo pitanje?", "Delete this question permanently?") });
        if (!confirmed)
        {
            return;
        }

        var response = await Http.DeleteAsync($"/api/admin/questions/{id}");
        if (response.IsSuccessStatusCode)
        {
            await LoadQuestions();
            flashMessage = L("Pitanje je uspješno obrisano.", "Question deleted successfully.");
            flashIsError = false;
        }
        else
        {
            flashMessage = L("Brisanje nije uspjelo", "Delete failed") + $" ({response.StatusCode}).";
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
        var confirmed = await JS.InvokeAsync<bool>("confirm", new object?[] { $"{L("Obrisati učenika", "Delete student")} '{studentName}'?" });
        if (!confirmed)
        {
            return;
        }

        var response = await Http.DeleteAsync($"/api/admin/students/{studentId}");
        if (response.IsSuccessStatusCode)
        {
            await LoadStudents();
            await LoadMetrics();
            flashMessage = L("Učenik je uspješno obrisan.", "Student deleted successfully.");
            flashIsError = false;
            return;
        }

        var errorRaw = await response.Content.ReadAsStringAsync();
        flashMessage = string.IsNullOrWhiteSpace(errorRaw)
            ? $"{L("Brisanje učenika nije uspjelo", "Student delete failed")} ({response.StatusCode})."
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
                flashMessage = L("Učenik je uspješno kreiran.", "Student created successfully.");
                flashIsError = false;
                break;
            case "question-created":
                flashMessage = L("Pitanje je uspješno kreirano.", "Question created successfully.");
                flashIsError = false;
                break;
            case "question-updated":
                flashMessage = L("Pitanje je uspješno ažurirano.", "Question updated successfully.");
                flashIsError = false;
                break;
            case "student-updated":
                flashMessage = L("Učenik je uspješno ažuriran.", "Student updated successfully.");
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

    private void HandleLanguageChanged()
    {
        _ = InvokeAsync(async () =>
        {
            await LoadQuestions();
            StateHasChanged();
        });
    }

    public void Dispose()
    {
        UiPrefs.Changed -= HandleLanguageChanged;
    }
}
