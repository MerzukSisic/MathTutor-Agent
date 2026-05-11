namespace AiAgents.MathTutorAgent.ML.Services;

public class MlTrainingResult
{
    public bool Success { get; set; }
    public string? KnowledgeTracingModelPath { get; set; }
    public string? TopicClassifierModelPath { get; set; }
    public int KnowledgeTracingSamplesCount { get; set; }
    public int TopicClassificationSamplesCount { get; set; }
    public int RealKnowledgeTracingSamplesCount { get; set; }
    public int RealTopicClassificationSamplesCount { get; set; }
    public string? ErrorMessage { get; set; }
}

public class MlReloadResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
}
