using AiAgents.Core;
using AiAgents.MathTutorAgent.Application.DTOs;
using AiAgents.MathTutorAgent.Application.Services;
using AiAgents.MathTutorAgent.Domain.Entities;
using AiAgents.MathTutorAgent.Domain.Enums;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AiAgents.MathTutorAgent.Application.Runners;

public class MathTutoringAgentRunner : SoftwareAgent<WorkItem, object, MathTickResult, object>
{
    private readonly WorkQueueService _queueService;
    private readonly ILogger<MathTutoringAgentRunner> _logger;

    public MathTutoringAgentRunner(
        WorkQueueService queueService,
        CurriculumService curriculumService,
        AssessmentService assessmentService,
        KnowledgeTracingService knowledgeTracingService,
        RevisionService revisionService,
        ExplanationService explanationService,
        ImageIngestionService imageIngestionService,
        ILogger<MathTutoringAgentRunner> logger)
        : base(
            new WorkQueuePerceptionSource(queueService),
            new MathTutoringPolicy(),
            new MathTutoringActuator(curriculumService, assessmentService, knowledgeTracingService, revisionService, explanationService, imageIngestionService, logger))
    {
        _queueService = queueService;
        _logger = logger;
    }

    // ✅ FIX: Override with correct signature
    public override async Task<MathTickResult?> StepAsync(CancellationToken cancellationToken)
    {
        try
        {
            // SENSE
            var workItem = await PerceptionSource.GetNextPerceptAsync(cancellationToken);
            if (workItem == null)
                return null;

            // THINK
            var action = await Policy.DecideAsync(workItem, cancellationToken);

            // ACT
            var result = await Actuator.ExecuteAsync(action, cancellationToken);

            // Mark done
            await _queueService.MarkDoneAsync(workItem.Id, JsonSerializer.Serialize(result), cancellationToken);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in agent tick");
            return null;
        }
    }
}

// ========== PERCEPTION SOURCE ==========
public class WorkQueuePerceptionSource : IPerceptionSource<WorkItem>
{
    private readonly WorkQueueService _queueService;

    public WorkQueuePerceptionSource(WorkQueueService queueService)
    {
        _queueService = queueService;
    }

    public async Task<WorkItem?> GetNextPerceptAsync(CancellationToken ct)
    {
        return await _queueService.DequeueNextAsync(ct);
    }
}

// ========== POLICY ==========
public class MathTutoringPolicy : IPolicy<WorkItem, object>
{
    public Task<object> DecideAsync(WorkItem percept, CancellationToken ct)
    {
        return Task.FromResult<object>(percept);
    }
}

// ========== ACTUATOR ==========
public class MathTutoringActuator : IActuator<object, MathTickResult>
{
    private readonly CurriculumService _curriculumService;
    private readonly AssessmentService _assessmentService;
    private readonly KnowledgeTracingService _knowledgeTracingService;
    private readonly RevisionService _revisionService;
    private readonly ExplanationService _explanationService;
    private readonly ImageIngestionService _imageIngestionService;
    private readonly ILogger _logger;

    public MathTutoringActuator(
        CurriculumService curriculumService,
        AssessmentService assessmentService,
        KnowledgeTracingService knowledgeTracingService,
        RevisionService revisionService,
        ExplanationService explanationService,
        ImageIngestionService imageIngestionService,
        ILogger logger)
    {
        _curriculumService = curriculumService;
        _assessmentService = assessmentService;
        _knowledgeTracingService = knowledgeTracingService;
        _revisionService = revisionService;
        _explanationService = explanationService;
        _imageIngestionService = imageIngestionService;
        _logger = logger;
    }

    public async Task<MathTickResult> ExecuteAsync(object action, CancellationToken ct)
    {
        var workItem = (WorkItem)action;

        return workItem.Type switch
        {
            WorkItemType.NextQuestion => await HandleNextQuestionAsync(workItem, ct),
            WorkItemType.SubmitAnswer => await HandleSubmitAnswerAsync(workItem, ct),
            WorkItemType.Explain => await HandleExplainAsync(workItem, ct),
            WorkItemType.UploadImage => await HandleUploadImageAsync(workItem, ct),
            _ => throw new InvalidOperationException($"Unknown work item type: {workItem.Type}")
        };
    }

    private async Task<MathTickResult> HandleNextQuestionAsync(WorkItem workItem, CancellationToken ct)
    {
        var needsRevision = await _revisionService.ShouldInjectRevisionAsync(workItem.StudentId, ct);
        int? topicId = null;

        if (needsRevision)
        {
            var revisionTopic = await _revisionService.PickRevisionTopicAsync(workItem.StudentId, ct);
            topicId = revisionTopic?.Id;
        }

        if (topicId == null)
        {
            var nextTopic = await _curriculumService.GetNextTopicAsync(workItem.StudentId, ct);
            topicId = nextTopic?.Id;
        }

        if (topicId == null)
        {
            return new MathTickResult
            {
                WorkItemId = workItem.Id,
                Type = workItem.Type,
                StudentId = workItem.StudentId,
                Outcome = TickOutcome.NoTopicsAvailable
            };
        }

        var question = await _assessmentService.SelectNextQuestionAsync(workItem.StudentId, topicId.Value, ct);
        if (question == null)
        {
            return new MathTickResult
            {
                WorkItemId = workItem.Id,
                Type = workItem.Type,
                StudentId = workItem.StudentId,
                Outcome = TickOutcome.NoQuestionsAvailable
            };
        }

        await _revisionService.UpdateScheduleAsync(workItem.StudentId, topicId.Value, ct);

        // ✅ FIX: Use proper DTO construction
        return new MathTickResult
        {
            WorkItemId = workItem.Id,
            Type = workItem.Type,
            StudentId = workItem.StudentId,
            Outcome = TickOutcome.QuestionGenerated,
            TopicId = topicId.Value,
            UiPayload = new QuestionDto
            {
                Id = question.Id,
                TopicId = question.TopicId,
                QuestionText = question.QuestionText,
                Difficulty = question.Difficulty
            }
        };
    }

    private async Task<MathTickResult> HandleSubmitAnswerAsync(WorkItem workItem, CancellationToken ct)
    {
        var payload = JsonSerializer.Deserialize<SubmitAnswerPayloadDto>(workItem.PayloadJson);
        if (payload == null)
            throw new InvalidOperationException("Invalid payload");

        var question = await _assessmentService.GetQuestionAsync(payload.QuestionId, ct);
        if (question == null)
            throw new InvalidOperationException($"Question {payload.QuestionId} not found");

        var isCorrect = await _assessmentService.EvaluateAnswerAsync(question, payload.Answer, ct);
        
        var attempt = await _assessmentService.SaveAttemptAsync(
            workItem.StudentId,
            payload.QuestionId,
            isCorrect,
            payload.TimeMs,
            payload.Answer,
            ct);

        var previousMastery = await _knowledgeTracingService.GetMasteryScoreAsync(workItem.StudentId, question.TopicId, ct);
        await _knowledgeTracingService.UpdateTopicStateAsync(workItem.StudentId, attempt, ct);
        var newMastery = await _knowledgeTracingService.GetMasteryScoreAsync(workItem.StudentId, question.TopicId, ct);

        var decision = await _assessmentService.DetermineAdvanceDecisionAsync(workItem.StudentId, question.TopicId, ct);

        return new MathTickResult
        {
            WorkItemId = workItem.Id,
            Type = workItem.Type,
            StudentId = workItem.StudentId,
            Outcome = TickOutcome.AnswerEvaluated,
            TopicId = question.TopicId,
            Decision = decision,
            MasteryDelta = newMastery - previousMastery,
            UiPayload = new
            {
                IsCorrect = isCorrect,
                CorrectAnswer = question.CorrectAnswer,
                MasteryScore = newMastery,
                Decision = decision.ToString()
            }
        };
    }

    private async Task<MathTickResult> HandleExplainAsync(WorkItem workItem, CancellationToken ct)
    {
        var payload = JsonSerializer.Deserialize<ExplainPayloadDto>(workItem.PayloadJson);
        if (payload == null)
            throw new InvalidOperationException("Invalid payload");

        var explanation = await _explanationService.RetrieveAndComposeExplanationAsync(
            workItem.StudentId,
            payload.QuestionId,
            payload.TopicId,
            payload.ErrorTag,
            ct);

        return new MathTickResult
        {
            WorkItemId = workItem.Id,
            Type = workItem.Type,
            StudentId = workItem.StudentId,
            Outcome = TickOutcome.ExplanationReady,
            UiPayload = explanation
        };
    }

    private async Task<MathTickResult> HandleUploadImageAsync(WorkItem workItem, CancellationToken ct)
    {
        var payload = JsonSerializer.Deserialize<UploadImagePayloadDto>(workItem.PayloadJson);
        if (payload == null)
            throw new InvalidOperationException("Invalid payload");

        var imageBytes = Convert.FromBase64String(payload.ImageBase64);
        var imageNote = await _imageIngestionService.IngestImageAsync(
            workItem.StudentId,
            imageBytes,
            payload.FileName,
            ct);

        return new MathTickResult
        {
            WorkItemId = workItem.Id,
            Type = workItem.Type,
            StudentId = workItem.StudentId,
            Outcome = TickOutcome.ImageIndexed,
            UiPayload = new
            {
                ImageNoteId = imageNote.Id,
                ExtractedText = imageNote.ExtractedText,
                Summary = imageNote.Summary,
                Tags = imageNote.Tags
            }
        };
    }
}

// ========== PAYLOAD DTOs ==========
public class SubmitAnswerPayloadDto
{
    public int QuestionId { get; set; }
    public string Answer { get; set; } = string.Empty;
    public int TimeMs { get; set; }
}

public class ExplainPayloadDto
{
    public int? QuestionId { get; set; }
    public int? TopicId { get; set; }
    public string? ErrorTag { get; set; }
}

public class UploadImagePayloadDto
{
    public string ImageBase64 { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
}