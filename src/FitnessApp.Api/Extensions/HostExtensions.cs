using Serilog;

namespace FitnessApp.Api.Extensions;

public static class HostExtensions
{
    public static IHostBuilder AddSerilogLogging(this IHostBuilder host) =>
        host.UseSerilog((ctx, services, cfg) => cfg
            .ReadFrom.Configuration(ctx.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext());
}
