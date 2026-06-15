namespace FitnessApp.Application.Interfaces;

public interface ITokenService
{
    (string Token, DateTime ExpiresAt) CreateToken(string userId, string email, string role);

    /// <summary>Generates a cryptographically random opaque refresh token and its expiry.</summary>
    (string Token, DateTime ExpiresAt) CreateRefreshToken();
}
