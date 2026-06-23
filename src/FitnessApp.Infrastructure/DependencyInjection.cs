using System.Text;
using FirebaseAdmin;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.Interfaces;
using FitnessApp.Application.Storage;
using FitnessApp.Infrastructure.Auth;
using FitnessApp.Infrastructure.Email;
using FitnessApp.Infrastructure.Identity;
using FitnessApp.Infrastructure.Notifications;
using FitnessApp.Infrastructure.Pdf;
using FitnessApp.Infrastructure.Persistence;
using FitnessApp.Infrastructure.Sharing;
using FitnessApp.Infrastructure.Storage;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using QuestPDF.Infrastructure;

namespace FitnessApp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        services.AddDbContext<Persistence.AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<Persistence.AppDbContext>());

        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequireDigit = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 6;
                options.Password.RequiredUniqueChars = 1;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        // Default lifespan of tokens from the default provider is 1 day; the only one we
        // actually generate is the password-reset token, so 2h is plenty and safer.
        services.Configure<DataProtectionTokenProviderOptions>(options =>
            options.TokenLifespan = TimeSpan.FromHours(2));

        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));
        services.Configure<GoogleAuthSettings>(configuration.GetSection("Google"));
        services.AddSingleton<ITokenService, TokenService>();

        var jwt = configuration.GetSection("Jwt").Get<JwtSettings>()
                  ?? throw new InvalidOperationException("Jwt settings are missing.");

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                    ClockSkew = TimeSpan.FromMinutes(1)
                };

                // SignalR (WebSockets) and Server-Sent Events streams can't send an Authorization
                // header, so they authenticate via the access_token query string instead.
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        var isTokenInQuery = path.StartsWithSegments("/hubs")
                            || (path.Value?.EndsWith("/admin/stats", StringComparison.OrdinalIgnoreCase) ?? false);
                        if (!string.IsNullOrEmpty(accessToken) && isTokenInQuery)
                            context.Token = accessToken;
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization();
        services.AddScoped<IUserDirectory, UserDirectory>();
        services.AddScoped<IIdentityAdminService, IdentityAdminService>();
        services.AddScoped<IUserRelationshipService, UserRelationshipService>();
        services.AddScoped<IUserProfileService, UserProfileService>();
        services.AddScoped<IAuthService, AuthService>();

        services.Configure<FirebaseSettings>(configuration.GetSection("Firebase"));
        var firebase = configuration.GetSection("Firebase").Get<FirebaseSettings>();
        if (firebase is not null && FirebaseApp.DefaultInstance is null)
        {
            GoogleCredential? credential = null;

            // 1) Inline JSON via config/env var (Firebase:CredentialsJson) — no file on disk.
            //    Best for prod: set the whole service-account JSON as a secret/env var.
            if (!string.IsNullOrWhiteSpace(firebase.CredentialsJson))
            {
                credential = GoogleCredential.FromJson(firebase.CredentialsJson);
            }
            // 2) File path (Firebase:CredentialsPath). Absolute path is used as-is;
            //    a relative path is resolved against the app folder (AppContext.BaseDirectory)
            //    exactly as written — no hidden subfolder is added.
            else if (!string.IsNullOrWhiteSpace(firebase.CredentialsPath))
            {
                var path = Path.IsPathRooted(firebase.CredentialsPath)
                    ? firebase.CredentialsPath
                    : Path.Combine(AppContext.BaseDirectory, firebase.CredentialsPath);

                if (File.Exists(path))
                    credential = GoogleCredential.FromFile(path);
            }

            if (credential is not null)
            {
                FirebaseApp.Create(new AppOptions
                {
                    Credential = credential,
                    ProjectId = firebase.ProjectId
                });
            }
        }

        services.AddScoped<IPushNotificationService, FirebasePushNotificationService>();

        services.Configure<RemindersSettings>(configuration.GetSection("Reminders"));
        services.AddHostedService<AppointmentReminderService>();

        services.Configure<StorageSettings>(configuration.GetSection("Storage"));
        services.Configure<DonationSettings>(configuration.GetSection("Donations"));
        services.AddSingleton<IEmailTemplateService, EmailTemplateService>();
        services.AddSingleton<IEmailService, EmailService>();
        services.AddSingleton<IFileStorageService, FileStorageService>();
        services.AddSingleton<IPlanShareTokenService, PlanShareTokenService>();
        services.AddSingleton<ITrainingPlanPdfService, TrainingPlanPdfService>();
        services.AddSingleton<INutritionPlanPdfService, NutritionPlanPdfService>();

        return services;
    }
}
