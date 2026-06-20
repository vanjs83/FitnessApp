namespace FitnessApp.Application.Features.Appointments;

internal static class AppointmentTime
{
    // The frontend sends the trainer's wall-clock time without a timezone, so the precise
    // "not in the past" check belongs in the browser (which knows the user's real local now).
    // This is a coarse server-side safety net for direct API calls: it only rejects times
    // further in the past than the largest possible timezone offset, so a valid near-future
    // booking is never wrongly refused regardless of where the caller is.
    private const int MaxTimezoneOffsetHours = 14;

    public static bool IsClearlyInPast(DateTime startsAt)
        => startsAt < DateTime.UtcNow.AddHours(-MaxTimezoneOffsetHours);
}
