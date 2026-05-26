using FitnessApp.Domain.Entities;

namespace FitnessApp.Application.Interfaces;

public interface INutritionPlanPdfService
{
    byte[] Generate(NutritionPlan plan, string? clientName, string? trainerName);
}
