using FitnessApp.Domain.Common;
using FitnessApp.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace FitnessApp.Api.Data;

public static class SuperAdminSeeder
{
    public static async Task SeedAsync(UserManager<ApplicationUser> userManager, IConfiguration configuration)
    {
        var section = configuration.GetSection("SuperAdmin");
        var email = section["Email"];
        var password = section["Password"];
        var fullName = section["FullName"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return;

        var existing = await userManager.FindByEmailAsync(email);
        if (existing != null)
        {
            if (!await userManager.IsInRoleAsync(existing, Roles.SuperAdmin))
                await userManager.AddToRoleAsync(existing, Roles.SuperAdmin);
            return;
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = fullName,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, password);
        if (result.Succeeded)
            await userManager.AddToRoleAsync(user, Roles.SuperAdmin);
    }
}
