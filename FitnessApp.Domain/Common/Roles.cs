namespace FitnessApp.Domain.Common;

public static class Roles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Trainer = "Trainer";
    public const string Client = "Client";

    public static readonly string[] All = { SuperAdmin, Trainer, Client };
    public static readonly string[] SelfRegisterable = { Trainer, Client };
}
