using Coravel.Queuing.Interfaces;
using FitnessApp.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FitnessApp.Infrastructure.Scheduling;

/// <summary>
/// Coravel-backed <see cref="IMessageScheduler"/>. Puts the work on the Coravel queue and runs it
/// later against a freshly-resolved service, guarded so a single failed send never tears down the
/// queue consumer.
/// </summary>
public sealed class MessageScheduler : IMessageScheduler
{
    private readonly IQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MessageScheduler> _logger;

    public MessageScheduler(IQueue queue, IServiceScopeFactory scopeFactory, ILogger<MessageScheduler> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public void Schedule<TService>(Func<TService, Task> work) where TService : notnull
        => _queue.QueueAsyncTask(async () =>
        {
            try
            {
                // Fresh scope per run — never capture the caller's request scope, which is disposed
                // once the response is sent.
                using var scope = _scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<TService>();
                await work(service);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Queued job for {Service} failed.", typeof(TService).Name);
            }
        });
}
