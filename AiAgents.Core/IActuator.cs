namespace AiAgents.Core;

public interface IActuator<in TAction, TResult>
{
    Task<TResult> ExecuteAsync(TAction action, CancellationToken cancellationToken = default);
}