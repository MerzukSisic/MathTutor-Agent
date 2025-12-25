namespace AiAgents.Core;

public interface ILearningComponent<in TExperience>
{
    Task LearnFromAsync(TExperience experience, CancellationToken cancellationToken = default);
}