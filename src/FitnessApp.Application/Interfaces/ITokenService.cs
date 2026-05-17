namespace FitnessApp.Application.Interfaces;

public interface ITokenService
{
    (string Token, DateTime ExpiresAt) CreateToken(string userId, string email, string role);
}
