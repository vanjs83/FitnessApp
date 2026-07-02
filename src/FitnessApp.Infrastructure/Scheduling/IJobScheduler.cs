namespace FitnessApp.Infrastructure.Scheduling;

/// <summary>
/// Injectable service that owns the Coravel schedule. <see cref="Start"/> wires every registered
/// <see cref="IScheduledJob"/> into the scheduler and is called once after the host is built. The
/// scheduler stays decoupled from any feature — jobs plug in via <c>AddScheduledJob&lt;T&gt;</c>.
/// </summary>
public interface IJobScheduler
{
    void Start();
}
