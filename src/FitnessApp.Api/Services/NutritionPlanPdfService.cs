using FitnessApp.Domain.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FitnessApp.Api.Services;

public class NutritionPlanPdfService
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

    private static readonly Dictionary<MealType, string> MealLabels = new()
    {
        [MealType.Breakfast] = "Doručak",
        [MealType.Snack1] = "Užina 1",
        [MealType.Lunch] = "Ručak",
        [MealType.Snack2] = "Užina 2",
        [MealType.Dinner] = "Večera",
        [MealType.LateSnack] = "Kasna užina",
        [MealType.Other] = "Ostalo"
    };

    public byte[] Generate(NutritionPlan plan, string? clientName, string? trainerName)
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
                    col.Item().Text($"Plan prehrane: {plan.Name}").FontSize(20).SemiBold();
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

                    if (!string.IsNullOrWhiteSpace(plan.Notes))
                    {
                        col.Item().Background(Colors.Grey.Lighten4).Padding(10).Column(b =>
                        {
                            b.Item().Text("Napomene trenera").SemiBold();
                            b.Item().PaddingTop(3).Text(plan.Notes);
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
                            var totals = ComputeDayTotals(day);

                            dayBlock.Item().Text(t =>
                            {
                                t.Span($"{DayNames[day.DayOfWeek]}").SemiBold().FontSize(14);
                                if (!string.IsNullOrWhiteSpace(day.Label))
                                    t.Span($"  — {day.Label}").FontColor(Colors.Grey.Darken1);
                            });

                            dayBlock.Item().PaddingTop(2).Text(t =>
                            {
                                t.Span("Ukupno: ").FontColor(Colors.Grey.Darken1);
                                t.Span($"{totals.Calories} kcal").SemiBold();
                                t.Span($" · P {totals.Protein:0}g · UH {totals.Carbs:0}g · M {totals.Fat:0}g").FontColor(Colors.Grey.Darken1);
                                if (day.TotalCaloriesTarget.HasValue)
                                {
                                    t.Span($"  · cilj ").FontColor(Colors.Grey.Darken1);
                                    t.Span($"{day.TotalCaloriesTarget.Value} kcal").SemiBold();
                                }
                            });

                            if (!string.IsNullOrWhiteSpace(day.Notes))
                            {
                                dayBlock.Item().PaddingTop(2).Text(day.Notes).Italic().FontColor(Colors.Grey.Darken2);
                            }

                            var meals = day.Meals.OrderBy(m => m.Order).ToList();
                            if (meals.Count == 0)
                            {
                                dayBlock.Item().PaddingTop(4).Text("— bez obroka —").Italic().FontColor(Colors.Grey.Medium);
                                return;
                            }

                            foreach (var meal in meals)
                            {
                                dayBlock.Item().PaddingTop(6).Column(mb =>
                                {
                                    var mt = ComputeMealTotals(meal);
                                    mb.Item().Text(t =>
                                    {
                                        t.Span(MealLabels.TryGetValue(meal.MealType, out var ml) ? ml : meal.MealType.ToString()).SemiBold();
                                        if (meal.Time.HasValue)
                                            t.Span($"  · {meal.Time.Value:HH\\:mm}").FontColor(Colors.Grey.Darken1);
                                        t.Span($"   ({mt.Calories} kcal · P {mt.Protein:0}g · UH {mt.Carbs:0}g · M {mt.Fat:0}g)").FontColor(Colors.Grey.Darken1).FontSize(9);
                                    });

                                    if (!string.IsNullOrWhiteSpace(meal.Notes))
                                    {
                                        mb.Item().PaddingTop(2).Text(meal.Notes).Italic().FontColor(Colors.Grey.Darken2).FontSize(10);
                                    }

                                    var items = meal.Items.OrderBy(i => i.Order).ToList();
                                    if (items.Count == 0)
                                    {
                                        mb.Item().PaddingTop(2).Text("— bez stavki —").Italic().FontColor(Colors.Grey.Medium).FontSize(10);
                                        return;
                                    }

                                    mb.Item().PaddingTop(4).Table(table =>
                                    {
                                        table.ColumnsDefinition(c =>
                                        {
                                            c.RelativeColumn(5);
                                            c.RelativeColumn(2);
                                            c.RelativeColumn(2);
                                            c.RelativeColumn(2);
                                            c.RelativeColumn(2);
                                            c.RelativeColumn(2);
                                        });
                                        table.Header(h =>
                                        {
                                            h.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Namirnica").SemiBold().FontSize(10);
                                            h.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Količina").SemiBold().FontSize(10);
                                            h.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("kcal").SemiBold().FontSize(10);
                                            h.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("P (g)").SemiBold().FontSize(10);
                                            h.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("UH (g)").SemiBold().FontSize(10);
                                            h.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("M (g)").SemiBold().FontSize(10);
                                        });

                                        foreach (var it in items)
                                        {
                                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(it.Description).FontSize(10);
                                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(it.Quantity ?? "").FontSize(10);
                                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(it.Calories?.ToString() ?? "").FontSize(10);
                                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(it.ProteinG?.ToString("0.#") ?? "").FontSize(10);
                                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(it.CarbsG?.ToString("0.#") ?? "").FontSize(10);
                                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(it.FatG?.ToString("0.#") ?? "").FontSize(10);
                                        }
                                    });
                                });
                            }
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

    private record Totals(int Calories, decimal Protein, decimal Carbs, decimal Fat);

    private static Totals ComputeMealTotals(Meal m)
    {
        var cal = m.Items.Sum(i => i.Calories ?? 0);
        var p = m.Items.Sum(i => i.ProteinG ?? 0);
        var c = m.Items.Sum(i => i.CarbsG ?? 0);
        var f = m.Items.Sum(i => i.FatG ?? 0);
        return new Totals(cal, p, c, f);
    }

    private static Totals ComputeDayTotals(NutritionDay d)
    {
        var cal = 0;
        decimal p = 0, c = 0, f = 0;
        foreach (var m in d.Meals)
        {
            var t = ComputeMealTotals(m);
            cal += t.Calories;
            p += t.Protein;
            c += t.Carbs;
            f += t.Fat;
        }
        return new Totals(cal, p, c, f);
    }
}
