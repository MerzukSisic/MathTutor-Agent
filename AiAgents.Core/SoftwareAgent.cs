namespace AiAgents.Core;

public abstract class SoftwareAgent<TPercept, TAction, TResult, TExperience>(
    IPerceptionSource<TPercept> perceptionSource,
    IPolicy<TPercept, TAction> policy,
    IActuator<TAction, TResult> actuator,
    ILearningComponent<TExperience>? learningComponent = null)
{
    protected IPerceptionSource<TPercept> PerceptionSource { get; } = perceptionSource;
    protected IPolicy<TPercept, TAction> Policy { get; } = policy;
    protected IActuator<TAction, TResult> Actuator { get; } = actuator;
    protected ILearningComponent<TExperience>? LearningComponent { get; } = learningComponent;

    /// <summary>
    /// Performs one agent step: Sense → Think → Act → Learn
    /// Returns null if no work available
    /// </summary>
    public abstract Task<TResult?> StepAsync(CancellationToken cancellationToken = default);
}