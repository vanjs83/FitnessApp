using FitnessApp.Domain.Entities;

namespace FitnessApp.Application.Interfaces;

public interface ITrainingPlanPdfService
{
    byte[] Generate(TrainingPlan plan, string? clientName, string? trainerName);
}
