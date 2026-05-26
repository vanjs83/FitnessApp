using FitnessApp.Application.Interfaces;
using FitnessApp.Domain.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FitnessApp.Infrastructure.Pdf;

public class TrainingPlanPdfService : ITrainingPlanPdfService
{
    private static readonly Dictionary<DayOfWeek, string> DayNames = new()
    {
        [DayOfWeek.Monday] = "Ponedjeljak",
        [DayOfWeek.Tuesday] = "Utorak",
        [DayOfWeek.Wednesday] = "Srijeda",
        [DayOfWeek.Thursday] = "Četvrtak",
        [DayOfWeek.Friday] = "Petak",
        [DayOfWeek.Saturday] = "Subota",
        [DayOfWeek.Sunday] = "Nedjelja"
    };

    private static string FormatDuration(int seconds)
    {
        if (seconds < 60) return $"{seconds}s";
        var minutes = seconds / 60.0;
        if (seconds % 60 == 0) return $"{seconds / 60} min";
        return $"{minutes.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture).Replace('.', ',')} min";
    }

    public byte[] Generate(TrainingPlan plan, string? clientName, string? trainerName)
    {
        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(t => t.FontSize(11).FontColor(Colors.Grey.Darken4));

                page.Header().Column(col =>
                {
                    col.Item().Text($"Plan treninga: {plan.Name}").FontSize(20).SemiBold();
                    col.Item().Text(t =>
                    {
                        t.Span("Period: ").SemiBold();
                        t.Span($"{plan.StartDate:dd.MM.yyyy.} – {plan.EndDate:dd.MM.yyyy.}");
                    });
                    col.Item().Text(t =>
                    {
                        t.Span("Klijent: ").SemiBold();
                        t.Span(clientName ?? "—");
                    });
                    col.Item().Text(t =>
                    {
                        t.Span("Trener: ").SemiBold();
                        t.Span(trainerName ?? "—");
                    });
                    col.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Spacing(14);

                    if (!string.IsNullOrWhiteSpace(plan.TrainerExpectations))
                    {
                        col.Item().Background(Colors.Grey.Lighten4).Padding(10).Column(b =>
                        {
                            b.Item().Text("Očekivanja trenera").SemiBold();
                            b.Item().PaddingTop(3).Text(plan.TrainerExpectations);
                        });
                    }

                    var orderedDays = plan.Days
                        .OrderBy(d => ((int)d.DayOfWeek + 6) % 7)
                        .ToList();

                    if (orderedDays.Count == 0)
                    {
                        col.Item().Text("Plan još nema dana.").Italic().FontColor(Colors.Grey.Medium);
                    }

                    foreach (var day in orderedDays)
                    {
                        col.Item().Column(dayBlock =>
                        {
                            dayBlock.Item().Text(t =>
                            {
                                t.Span($"{DayNames[day.DayOfWeek]}").SemiBold().FontSize(14);
                                if (!string.IsNullOrWhiteSpace(day.Label))
                                    t.Span($"  — {day.Label}").FontColor(Colors.Grey.Darken1);
                            });

                            if (!string.IsNullOrWhiteSpace(day.Notes))
                            {
                                dayBlock.Item().PaddingTop(2).Text(day.Notes).Italic().FontColor(Colors.Grey.Darken2);
                            }

                            var exercises = day.Exercises.OrderBy(e => e.Order).ToList();
                            if (exercises.Count == 0)
                            {
                                dayBlock.Item().PaddingTop(4).Text("— bez vježbi —").Italic().FontColor(Colors.Grey.Medium);
                                return;
                            }

                            dayBlock.Item().PaddingTop(6).Table(table =>
                            {
                                table.ColumnsDefinition(c =>
                                {
                                    c.ConstantColumn(25);
                                    c.RelativeColumn(4);
                                    c.RelativeColumn(2);
                                    c.RelativeColumn(2);
                                    c.RelativeColumn(3);
                                });

                                table.Header(h =>
                                {
                                    h.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("#").SemiBold();
                                    h.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Vježba").SemiBold();
                                    h.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Serije × pon./vrijeme").SemiBold();
                                    h.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Težina / odmor").SemiBold();
                                    h.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Napomene").SemiBold();
                                });

                                int idx = 1;
                                foreach (var ex in exercises)
                                {
                                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(idx.ToString());
                                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(ex.Exercise?.Name ?? "(vježba)");

                                    string sxr;
                                    string rightStr;
                                    if (ex.TargetDurationSeconds.HasValue)
                                    {
                                        sxr = $"{ex.TargetSets} × {FormatDuration(ex.TargetDurationSeconds.Value)}";
                                        var weight = ex.TargetWeightKg > 0 ? $"{ex.TargetWeightKg} kg" : "";
                                        var rest = ex.RestSeconds.HasValue ? $"odmor {ex.RestSeconds}s" : "";
                                        rightStr = string.Join(", ", new[] { weight, rest }.Where(s => !string.IsNullOrEmpty(s)));
                                        if (string.IsNullOrEmpty(rightStr)) rightStr = "—";
                                    }
                                    else
                                    {
                                        sxr = $"{ex.TargetSets} × {ex.TargetReps}";
                                        var weightStr = ex.TargetWeightKg > 0 ? $"{ex.TargetWeightKg} kg" : "—";
                                        var restStr = ex.RestSeconds.HasValue ? $", odmor {ex.RestSeconds}s" : "";
                                        rightStr = weightStr + restStr;
                                    }
                                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(sxr);
                                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(rightStr);

                                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(ex.Notes ?? "");
                                    idx++;
                                }
                            });
                        });
                    }
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.DefaultTextStyle(s => s.FontSize(9).FontColor(Colors.Grey.Medium));
                    t.Span($"Generirano: {DateTime.Now:dd.MM.yyyy. HH:mm} · FitnessApp · stranica ");
                    t.CurrentPageNumber();
                    t.Span(" / ");
                    t.TotalPages();
                });
            });
        });

        return doc.GeneratePdf();
    }
}
