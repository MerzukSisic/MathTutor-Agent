using System.Text.Json;
using AiAgents.MathTutorAgent.Application.DTOs;
using AiAgents.MathTutorAgent.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using System.Net.Http.Json;
using Radzen;

namespace AiAgents.MathTutorAgent.Web.Components.Pages;

public partial class Admin
{
    private PerformanceMetricsDto? metrics;
    private List<AdminQuestionDto>? questions;
    private List<StudentDto>? students;
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
            NotifyError(L("Ne mogu učitati metrike performansi.", "Unable to load performance metrics."));
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
            NotifyError(L("Ne mogu učitati listu pitanja.", "Unable to load questions."));
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
            NotifyError(L("Ne mogu učitati listu učenika.", "Unable to load students."));
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
            NotifySuccess(L("Uspješno ste obrisali pitanje.", "You successfully deleted the question."));
        }
        else
        {
            NotifyError($"{L("Brisanje pitanja nije uspjelo.", "Failed to delete question.")} ({response.StatusCode})");
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
            NotifySuccess(L("Uspješno ste obrisali učenika.", "You successfully deleted the student."));
            return;
        }

        var errorRaw = await response.Content.ReadAsStringAsync();
        var errorMessage = string.IsNullOrWhiteSpace(errorRaw)
            ? $"{L("Brisanje učenika nije uspjelo", "Student delete failed")} ({response.StatusCode})."
            : ExtractApiErrorMessage(errorRaw);
        NotifyError(errorMessage);
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
                NotifySuccess(L("Uspješno ste kreirali učenika.", "You successfully created the student."));
                break;
            case "question-created":
                NotifySuccess(L("Uspješno ste kreirali pitanje.", "You successfully created the question."));
                break;
            case "question-updated":
                NotifySuccess(L("Uspješno ste editovali pitanje.", "You successfully edited the question."));
                break;
            case "student-updated":
                NotifySuccess(L("Uspješno ste editovali učenika.", "You successfully edited the student."));
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

    private void NotifySuccess(string detail)
        => Notifications.Notify(NotificationSeverity.Success, L("Uspješno", "Success"), detail, 4500);

    private void NotifyError(string detail)
        => Notifications.Notify(NotificationSeverity.Error, L("Neuspješno", "Failed"), detail, 6000);
}
