namespace FitnessApp.Application.Interfaces;

public interface IPlanShareTokenService
{
    string Create(int planId, int? ttlHours = null);
    string CreateForKind(string kind, int planId, int? ttlHours = null);
    bool TryValidate(string token, out int planId);
    bool TryValidateForKind(string expectedKind, string token, out int planId);
}
