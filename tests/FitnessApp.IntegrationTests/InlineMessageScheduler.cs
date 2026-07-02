using FitnessApp.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace FitnessApp.IntegrationTests;

/// <summary>
/// Test double for <see cref="IMessageScheduler"/> that runs the work inline (synchronously, in a
/// fresh scope) instead of putting it on Coravel's background queue. Keeps integration tests
/// deterministic — the send completes during the request — and avoids racing Coravel's queue
/// shutdown against the factory's disposal.
/// </summary>
public sealed class InlineMessageScheduler : IMessageScheduler
{
    private readonly IServiceScopeFactory _scopeFactory;

    public InlineMessageScheduler(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    public void Schedule<TService>(Func<TService, Task> work) where TService : notnull
    {
        using var scope = _scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<TService>();
        // Fire-and-forget in prod; here we run it to completion so tests are deterministic. Swallow
        // like the real scheduler so a failing send never breaks the request.
        try { work(service).GetAwaiter().GetResult(); }
        catch { /* best-effort, mirror production */ }
    }
}
