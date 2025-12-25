namespace AiAgents.Core;

public interface IPolicy<in TPercept, TAction>
{
    Task<TAction> DecideAsync(TPercept percept, CancellationToken cancellationToken = default);
}