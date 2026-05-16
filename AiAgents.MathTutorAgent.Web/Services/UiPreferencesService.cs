using System.Globalization;

namespace AiAgents.MathTutorAgent.Web.Services;

public enum UiLanguage
{
    Bs,
    En
}

public enum UiTheme
{
    Light,
    Dark
}

public sealed class UiPreferencesService
{
    private static readonly Dictionary<string, (string Bs, string En)> Texts = new(StringComparer.OrdinalIgnoreCase)
    {
        ["app_name"] = ("MathTutor AI", "MathTutor AI"),
        ["page_dashboard"] = ("Kontrolna tabla", "Dashboard"),
        ["page_admin_panel"] = ("Admin panel", "Admin Panel"),
        ["page_add_student"] = ("Dodaj učenika", "Add Student"),
        ["page_add_question"] = ("Dodaj pitanje", "Add Question"),
        ["page_edit_student"] = ("Uredi učenika", "Edit Student"),
        ["page_edit_question"] = ("Uredi pitanje", "Edit Question"),
        ["page_practice_quiz"] = ("Kviz vježba", "Practice Quiz"),
        ["page_student_profile"] = ("Profil učenika", "Student Profile"),
        ["page_sign_in"] = ("Prijava", "Sign In"),
        ["page_register"] = ("Registracija", "Register"),
        ["page_confirm_email"] = ("Potvrda emaila", "Confirm Email"),
        ["page_forgot_password"] = ("Zaboravljena lozinka", "Forgot Password"),
        ["page_reset_password"] = ("Reset lozinke", "Reset Password"),
        ["page_resend_confirmation"] = ("Ponovo pošalji potvrdu", "Resend Confirmation"),
        ["toggle_menu"] = ("Meni", "Menu"),
        ["expand_sidebar"] = ("Proširi meni", "Expand Sidebar"),
        ["collapse_sidebar"] = ("Skupi meni", "Collapse Sidebar"),
        ["switch_theme"] = ("Promijeni temu", "Switch Theme"),
        ["switch_language"] = ("Promijeni jezik", "Switch Language"),
        ["theme_light"] = ("Svijetla", "Light"),
        ["theme_dark"] = ("Tamna", "Dark"),
        ["language_bs"] = ("Bosanski", "Bosnian"),
        ["language_en"] = ("Engleski", "English"),
        ["error_occurred"] = ("Došlo je do greške", "An error occurred"),
        ["reload"] = ("Učitaj ponovo", "Reload"),
        ["main"] = ("Glavno", "Main"),
        ["management"] = ("Upravljanje", "Management"),
        ["learning"] = ("Učenje", "Learning"),
        ["account"] = ("Nalog", "Account"),
        ["dashboard"] = ("Kontrolna tabla", "Dashboard"),
        ["admin_panel"] = ("Admin panel", "Admin Panel"),
        ["practice_quiz"] = ("Kviz vježba", "Practice Quiz"),
        ["my_profile"] = ("Moj profil", "My Profile"),
        ["login"] = ("Prijava", "Login"),
        ["register"] = ("Registracija", "Register"),
        ["logout"] = ("Odjava", "Logout"),
        ["agent_online"] = ("Agent online", "Agent Online"),
        ["agent_offline"] = ("Agent offline", "Agent Offline"),
        ["welcome_title"] = ("Dobrodošli u MathTutor AI", "Welcome to MathTutor AI"),
        ["welcome_subtitle"] = ("Prati napredak u učenju i savladaj matematiku korak po korak", "Track your learning progress and master mathematics step by step"),
        ["start_learning"] = ("Počni učenje", "Start Learning"),
        ["select_student"] = ("Izaberi učenika", "Select Student"),
        ["search_student"] = ("Pretraži učenika...", "Search student..."),
        ["refresh"] = ("Osvježi", "Refresh"),
        ["loading_students"] = ("Učitavanje učenika...", "Loading students..."),
        ["showing_students"] = ("Prikaz @0 / @1 učenika", "Showing @0 / @1 students"),
        ["no_students_found"] = ("Nema pronađenih učenika", "No students found"),
        ["add_students"] = ("Dodaj učenike", "Add Students"),
        ["start_quiz"] = ("Pokreni kviz", "Start Quiz"),
        ["total_students"] = ("Ukupno učenika", "Total Students"),
        ["active_sessions"] = ("Aktivne sesije", "Active Sessions"),
        ["questions_completed"] = ("Riješenih pitanja", "Questions Completed"),
        ["agent_status"] = ("Status agenta", "Agent Status"),
        ["footer_note"] = ("MathTutor AI Agent © 2026 | Napravljeno sa .NET 9 i ML.NET", "MathTutor AI Agent © 2026 | Built with .NET 9 & ML.NET"),
        ["member_since"] = ("Član od @0", "Member since @0"),
        ["loading"] = ("Učitavanje...", "Loading..."),
        ["status_processing"] = ("Obrada", "Processing"),
        ["status_online"] = ("Online", "Online"),
        ["status_offline"] = ("Offline", "Offline")
    };

    public event Action? Changed;

    public UiLanguage Language { get; private set; } = UiLanguage.Bs;
    public UiTheme Theme { get; private set; } = UiTheme.Light;
    public bool SidebarCollapsed { get; private set; }

    public string LanguageCode => Language == UiLanguage.Bs ? "bs" : "en";
    public string ThemeCode => Theme == UiTheme.Dark ? "dark" : "light";

    public string T(string key)
    {
        if (Texts.TryGetValue(key, out var value))
        {
            return Language == UiLanguage.Bs ? value.Bs : value.En;
        }

        return key;
    }

    public string T(string key, params object[] args)
    {
        var template = T(key);
        for (var i = 0; i < args.Length; i++)
        {
            template = template.Replace($"@{i}", Convert.ToString(args[i], CultureInfo.InvariantCulture));
        }

        return template;
    }

    public void SetLanguage(string? code, bool notify = true)
    {
        Language = string.Equals(code, "en", StringComparison.OrdinalIgnoreCase)
            ? UiLanguage.En
            : UiLanguage.Bs;

        if (notify)
        {
            Changed?.Invoke();
        }
    }

    public void ToggleLanguage()
    {
        Language = Language == UiLanguage.Bs ? UiLanguage.En : UiLanguage.Bs;
        Changed?.Invoke();
    }

    public void SetTheme(string? code, bool notify = true)
    {
        Theme = string.Equals(code, "dark", StringComparison.OrdinalIgnoreCase)
            ? UiTheme.Dark
            : UiTheme.Light;

        if (notify)
        {
            Changed?.Invoke();
        }
    }

    public void ToggleTheme()
    {
        Theme = Theme == UiTheme.Dark ? UiTheme.Light : UiTheme.Dark;
        Changed?.Invoke();
    }

    public void SetSidebarCollapsed(bool collapsed, bool notify = true)
    {
        SidebarCollapsed = collapsed;
        if (notify)
        {
            Changed?.Invoke();
        }
    }
}
