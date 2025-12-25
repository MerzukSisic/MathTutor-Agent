namespace AiAgents.Core;

public interface IPerceptionSource<TPercept>
{
    Task<TPercept?> GetNextPerceptAsync(CancellationToken cancellationToken = default);
}