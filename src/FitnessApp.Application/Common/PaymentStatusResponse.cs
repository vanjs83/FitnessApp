namespace FitnessApp.Application.Common;

/// <summary>Returned by payment state transitions (claim / approve / revoke).</summary>
public record PaymentStatusResponse(string PaymentStatus);
