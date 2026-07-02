namespace FitnessApp.Application.Interfaces;

/// <summary>
/// Runs work fire-and-forget on a background queue. Instead of re-declaring every send method, the
/// caller passes the service it needs and a lambda invoking it — e.g.
/// <c>scheduler.Schedule&lt;IEmailService&gt;(mail =&gt; mail.SendAsync(to, subject, body))</c>. The
/// service is resolved in a fresh DI scope at run time, so handlers return without waiting on SMTP/Firebase.
/// </summary>
public interface IMessageScheduler
{
    /// <summary>
    /// Queues <paramref name="work"/> to run later against a freshly-resolved <typeparamref name="TService"/>.
    /// </summary>
    void Schedule<TService>(Func<TService, Task> work) where TService : notnull;
}
