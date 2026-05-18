using AiAgents.MathTutorAgent.Application.DTOs;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AiAgents.MathTutorAgent.Application.Services;

public class PdfExportService
{
    public byte[] GenerateStudentReport(StudentProfileDto profile, StudySessionStatsDto stats, string? languageCode = null)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var isBosnian = !string.Equals(languageCode, "en", StringComparison.OrdinalIgnoreCase);
        string T(string bs, string en) => isBosnian ? bs : en;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header()
                    .Text(T("MathTutor AI - Izvještaj o napretku učenika", "MathTutor AI - Student Progress Report"))
                    .SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);

                page.Content()
                    .PaddingVertical(1, Unit.Centimetre)
                    .Column(column =>
                    {
                        column.Spacing(20);

                        // Student Info
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text($"{T("Učenik", "Student")}: {profile.Name}").Bold();
                                col.Item().Text($"Email: {profile.Email}");
                            });
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text($"{T("Datum izvještaja", "Report Date")}: {DateTime.Now:yyyy-MM-dd}").AlignRight();
                            });
                        });

                        // Summary Stats
                        column.Item().Background(Colors.Grey.Lighten3).Padding(10).Column(col =>
                        {
                            col.Item().Text(T("Ukupna statistika", "Overall Statistics")).Bold().FontSize(14);
                            col.Item().PaddingTop(5).Text($"{T("Ukupno pokušaja", "Total Attempts")}: {profile.TotalAttempts}");
                            col.Item().Text($"{T("Tačni odgovori", "Correct Answers")}: {profile.CorrectAttempts}");
                            col.Item().Text($"{T("Tačnost", "Accuracy")}: {profile.AccuracyPercentage:F1}%");
                            col.Item().Text($"{T("Prosječno vrijeme", "Average Time")}: {profile.AverageTimeSeconds:F1} {T("sekundi", "seconds")}");
                        });

                        // Topic Progress
                        column.Item().Text(T("Vladanje temama", "Topic Mastery")).Bold().FontSize(14);
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text(T("Tema", "Topic")).Bold();
                                header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text(T("Nivo znanja", "Mastery Score")).Bold();
                                header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text(T("Pouzdanost", "Confidence")).Bold();
                            });

                            foreach (var topic in profile.TopicProgress)
                            {
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(topic.TopicName);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text($"{topic.MasteryScore:F1}");
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text($"{topic.Confidence:F2}");
                            }
                        });

                        // Daily Performance
                        if (stats.DailyStats.Any())
                        {
                            column.Item().Text(T("Dnevni učinak", "Daily Performance")).Bold().FontSize(14);
                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text(T("Datum", "Date")).Bold();
                                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text(T("Pokušaji", "Attempts")).Bold();
                                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text(T("Tačno", "Correct")).Bold();
                                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text(T("Prosj. vrijeme", "Avg Time")).Bold();
                                });

                                foreach (var day in stats.DailyStats.Take(10))
                                {
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(day.Date.ToString("yyyy-MM-dd"));
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(day.TotalAttempts.ToString());
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(day.CorrectAttempts.ToString());
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text($"{day.AverageTimeSeconds:F1}s");
                                }
                            });
                        }
                    });

                page.Footer()
                    .AlignCenter()
                    .Text(x =>
                    {
                        x.Span(T("Generisao MathTutor AI Agent • ", "Generated by MathTutor AI Agent • "));
                        x.CurrentPageNumber();
                        x.Span(" / ");
                        x.TotalPages();
                    });
            });
        });

        return document.GeneratePdf();
    }
}
