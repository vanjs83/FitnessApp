using System.Text.Json.Serialization;
using Asp.Versioning;
using FitnessApp.Api.Middleware;
using FitnessApp.Api.Services;
using FitnessApp.Application;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using SharpGrip.FluentValidation.AutoValidation.Mvc.Extensions;

namespace FitnessApp.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers(options =>
            {
                // Client-side cache profiles referenced by [ResponseCache(CacheProfileName = ...)]
                // on GET endpoints. These only emit Cache-Control headers (no server-side cache);
                // durations are tuned per data volatility and kept in one place.
                //   Reference  – rarely changes (exercise/template/trainer reference data)
                //   UserData   – user-bound lists/details that change occasionally
                //   Volatile   – frequently changing (chat, requests, live status counters)
                //   PublicShare– anonymous share links, cacheable by any (shared) cache
                // VaryByHeader = "Authorization" keys the cache on the JWT so one user's cached
                // response is never reused for another.
                options.CacheProfiles.Add("Reference",
                    new CacheProfile { Location = ResponseCacheLocation.Client, Duration = 300, VaryByHeader = "Authorization" });
                options.CacheProfiles.Add("UserData",
                    new CacheProfile { Location = ResponseCacheLocation.Client, Duration = 300, VaryByHeader = "Authorization" });
                options.CacheProfiles.Add("Volatile",
                    new CacheProfile { Location = ResponseCacheLocation.Client, Duration = 15, VaryByHeader = "Authorization" });
                options.CacheProfiles.Add("PublicShare",
                    new CacheProfile { Location = ResponseCacheLocation.Any, Duration = 300 });
            })
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

        services.AddFluentValidationAutoValidation();
        services.AddSignalR();

        services.AddApplication();
        services.AddInfrastructure(configuration);

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IChatNotifier, ChatNotifier>();

        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        services.AddApiVersioningConfigured();
        services.AddCorsPolicy();
        services.AddSwaggerWithJwt();

        return services;
    }

    private static IServiceCollection AddApiVersioningConfigured(this IServiceCollection services)
    {
        services
            .AddApiVersioning(options =>
            {
                // v1 is assumed when the caller omits the version, so existing /api/v1
                // routes resolve cleanly and future v2 can be added without breaking v1.
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
                options.ApiVersionReader = new UrlSegmentApiVersionReader();
            })
            .AddApiExplorer(options =>
            {
                // Produces group names like "v1" and substitutes {version} in the
                // route templates so Swagger shows concrete paths (/api/v1/...).
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });

        return services;
    }

    private static IServiceCollection AddCorsPolicy(this IServiceCollection services)
    {
        // Production frontend (fitness-application.com) plus local dev (Vite / Angular).
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
                policy.WithOrigins(
                        "https://fitness-application.com",
                        "https://www.fitness-application.com",
                        "http://localhost:5173",
                        "http://localhost:4200")
                      .AllowAnyHeader()
                      .AllowAnyMethod());
        });

        return services;
    }

    private static IServiceCollection AddSwaggerWithJwt(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "FitnessApp API", Version = "v1" });

            var jwtScheme = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Unesi JWT token (bez 'Bearer' prefiksa)."
            };
            c.AddSecurityDefinition("Bearer", jwtScheme);
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                    },
                    Array.Empty<string>()
                }
            });
        });

        return services;
    }
}
