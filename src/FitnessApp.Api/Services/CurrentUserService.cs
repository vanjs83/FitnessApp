using System.Security.Claims;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace FitnessApp.Api.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UserManager<ApplicationUser> _userManager;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor, UserManager<ApplicationUser> userManager)
    {
        _httpContextAccessor = httpContextAccessor;
        _userManager = userManager;
    }

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    public string UserId =>
        Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("No authenticated user in the current context.");

    public bool IsInRole(string role) => Principal?.IsInRole(role) ?? false;

    public string? PrimaryRole => Principal?.FindFirstValue(ClaimTypes.Role);

    public async Task<string?> GetTrainerIdAsync(CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(UserId);
        return user?.TrainerId;
    }
}
