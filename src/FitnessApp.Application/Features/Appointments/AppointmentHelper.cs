using System.Globalization;
using FitnessApp.Domain.Entities;

namespace FitnessApp.Application.Features.Appointments;

/// <summary>
/// Builds the localized push title/body/data payload for appointment and group-session events.
/// Recipient language isn't persisted yet, so copy defaults to Croatian (matching the other push
/// commands). An appointment can be individual or for a whole group — both are handled here.
/// </summary>
internal static class AppointmentHelper
{
    // Local-time without a timezone, shown as the trainer/client picked it.
    private static string When(DateTime startsAt) => startsAt.ToString("dd.MM.yyyy. HH:mm", CultureInfo.InvariantCulture);

    /// <summary>Trainer booked a confirmed individual session — notifies the client.</summary>
    public static (string Title, string Body, IDictionary<string, string> Data) Booked(Appointment a) =>
        ("Novi termin",
         $"Trener ti je zakazao termin {When(a.StartsAt)}.",
         Data("appointment", "booked", a.Id, a.StartsAt));

    /// <summary>Client proposed a session — notifies the trainer.</summary>
    public static (string Title, string Body, IDictionary<string, string> Data) Requested(Appointment a, string clientName) =>
        ("Zahtjev za termin",
         $"{clientName} traži termin {When(a.StartsAt)}.",
         Data("appointment", "requested", a.Id, a.StartsAt));

    /// <summary>Trainer booked a group session — notifies every member of the group.</summary>
    public static (string Title, string Body, IDictionary<string, string> Data) GroupBooked(GroupSession s, string groupName) =>
        ("Grupni trening",
         $"Zakazan je grupni trening '{groupName}' {When(s.StartsAt)}.",
         Data("groupSession", "booked", s.Id, s.StartsAt, groupName));

    /// <summary>Reminder that an individual session starts soon — notifies the client.</summary>
    public static (string Title, string Body, IDictionary<string, string> Data) Reminder(Appointment a) =>
        ("Podsjetnik na trening",
         $"Tvoj trening počinje uskoro — {When(a.StartsAt)}.",
         Data("appointment", "reminder", a.Id, a.StartsAt));

    /// <summary>Reminder that a group session starts soon — notifies each member.</summary>
    public static (string Title, string Body, IDictionary<string, string> Data) GroupReminder(GroupSession s, string groupName) =>
        ("Podsjetnik na trening",
         $"Grupni trening '{groupName}' počinje uskoro — {When(s.StartsAt)}.",
         Data("groupSession", "reminder", s.Id, s.StartsAt, groupName));

    private static IDictionary<string, string> Data(
        string type, string action, int id, DateTime startsAt, string? groupName = null)
    {
        var data = new Dictionary<string, string>
        {
            ["type"] = type,
            ["action"] = action,
            ["id"] = id.ToString(CultureInfo.InvariantCulture),
            ["startsAt"] = startsAt.ToString("o", CultureInfo.InvariantCulture)
        };
        if (groupName is not null) data["groupName"] = groupName;
        return data;
    }
}
