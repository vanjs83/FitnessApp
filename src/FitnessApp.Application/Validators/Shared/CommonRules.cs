using FluentValidation;

namespace FitnessApp.Application.Validators.Shared;

public static class CommonRules
{
    private static readonly HashSet<string> AllowedCurrencies = new(StringComparer.OrdinalIgnoreCase)
    {
        "EUR", "USD", "HRK", "BAM", "RSD"
    };

    public static IRuleBuilderOptions<T, string> ValidEmail<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty()
            .EmailAddress()
            .MaximumLength(254);

    public static IRuleBuilderOptions<T, string> ValidPassword<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty()
            .MinimumLength(6)
            .MaximumLength(100);

    public static IRuleBuilderOptions<T, string> ValidCurrency<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty()
            .MaximumLength(8)
            .Must(c => AllowedCurrencies.Contains(c))
            .WithMessage("Unsupported currency.");

    public static IRuleBuilderOptions<T, decimal> ValidPrice<T>(this IRuleBuilder<T, decimal> rule) =>
        rule.GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(99999);
}
