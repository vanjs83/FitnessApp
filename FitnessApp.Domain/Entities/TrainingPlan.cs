namespace FitnessApp.Domain.Entities;

public class TrainingPlan
{
    public int Id { get; set; }
    public string TrainerId { get; set; } = string.Empty;
    public string? ClientId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? TrainerExpectations { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsTemplate { get; set; }

    public decimal Price { get; set; }
    public string Currency { get; set; } = "EUR";
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
    public DateTime? PaymentClaimedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }

    public ICollection<TrainingDay> Days { get; set; } = new List<TrainingDay>();
}

public enum PaymentStatus
{
    Pending = 0,
    PaymentClaimed = 1,
    Approved = 2
}
