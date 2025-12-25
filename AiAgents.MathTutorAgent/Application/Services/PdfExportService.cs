using AiAgents.MathTutorAgent.Application.DTOs;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AiAgents.MathTutorAgent.Application.Services;

public class PdfExportService
{
    public byte[] GenerateStudentReport(StudentProfileDto profile, StudySessionStatsDto stats)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header()
                    .Text("MathTutor AI - Student Progress Report")
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
                                col.Item().Text($"Student: {profile.Name}").Bold();
                                col.Item().Text($"Email: {profile.Email}");
                            });
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text($"Report Date: {DateTime.Now:yyyy-MM-dd}").AlignRight();
                            });
                        });

                        // Summary Stats
                        column.Item().Background(Colors.Grey.Lighten3).Padding(10).Column(col =>
                        {
                            col.Item().Text("Overall Statistics").Bold().FontSize(14);
                            col.Item().PaddingTop(5).Text($"Total Attempts: {profile.TotalAttempts}");
                            col.Item().Text($"Correct Answers: {profile.CorrectAttempts}");
                            col.Item().Text($"Accuracy: {profile.AccuracyPercentage:F1}%");
                            col.Item().Text($"Average Time: {profile.AverageTimeSeconds:F1} seconds");
                        });

                        // Topic Progress
                        column.Item().Text("Topic Mastery").Bold().FontSize(14);
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
                                header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Topic").Bold();
                                header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Mastery Score").Bold();
                                header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Confidence").Bold();
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
                            column.Item().Text("Daily Performance").Bold().FontSize(14);
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
                                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Date").Bold();
                                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Attempts").Bold();
                                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Correct").Bold();
                                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Avg Time").Bold();
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
                        x.Span("Generated by MathTutor AI Agent • ");
                        x.CurrentPageNumber();
                        x.Span(" / ");
                        x.TotalPages();
                    });
            });
        });

        return document.GeneratePdf();
    }
}