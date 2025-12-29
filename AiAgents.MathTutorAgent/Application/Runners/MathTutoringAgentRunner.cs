using AiAgents.Core;
using AiAgents.MathTutorAgent.Application.DTOs;
using AiAgents.MathTutorAgent.Application.Services;
using AiAgents.MathTutorAgent.Domain.Entities;
using AiAgents.MathTutorAgent.Domain.Enums;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AiAgents.MathTutorAgent.Application.Runners;

public class MathTutoringAgentRunner(
    WorkQueueService queueService,
    CurriculumService curriculumService,
    AssessmentService assessmentService,
    KnowledgeTracingService knowledgeTracingService,
    RevisionService revisionService,
    ExplanationService explanationService,
    ImageIngestionService imageIngestionService,
    ValidationService validationService,
    ILogger<MathTutoringAgentRunner> logger)
    : SoftwareAgent<WorkItem, MathAction, MathTickResult, object>(
        new WorkQueuePerceptionSource(queueService),
        new MathTutoringPolicy(),
        new MathTutoringActuator(curriculumService, assessmentService, knowledgeTracingService, revisionService,
            explanationService, imageIngestionService, validationService, logger))
{
    // ✅ PATCH A: StepAsync with proper error handling
    public override async Task<MathTickResult?> StepAsync(CancellationToken cancellationToken)
    {
        WorkItem? workItem = null;
        
        try
        {
            // SENSE
            workItem = await PerceptionSource.GetNextPerceptAsync(cancellationToken);
            if (workItem == null)
                return null; // NoWork - semantically correct

            // THINK
            var action = await Policy.DecideAsync(workItem, cancellationToken);

            // ACT
            var result = await Actuator.ExecuteAsync(action, cancellationToken);

            // Mark done (idempotent - only if Processing)
            await queueService.MarkDoneAsync(workItem.Id, JsonSerializer.Serialize(result), cancellationToken);

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in agent tick for WorkItem {WorkItemId}", workItem?.Id ?? 0);
            
            // CRITICAL: Mark as failed so it doesn't stay stuck in Processing
            if (workItem != null)
            {
                await queueService.MarkFailedAsync(workItem.Id, ex.Message, cancellationToken);
            }

            // Return Failed outcome (NOT null - null means NoWork)
            return new MathTickResult
            {
                WorkItemId = workItem?.Id ?? 0,
                Type = workItem?.Type ?? WorkItemType.NextQuestion,
                StudentId = workItem?.StudentId ?? 0,
                Outcome = TickOutcome.Failed,
                UiPayload = new { Error = ex.Message, ErrorType = ex.GetType().Name }
            };
        }
    }
}

// ========== ACTION MODEL (Think output) ==========
public record MathAction(
    WorkItem WorkItem, 
    MathActionType Type, 
    string? Reason = null);

public enum MathActionType
{
    NextQuestion,
    SubmitAnswer,
    Explain,
    UploadImage,
    RejectInvalid
}

// ========== PERCEPTION SOURCE ==========
public class WorkQueuePerceptionSource(WorkQueueService queueService) : IPerceptionSource<WorkItem>
{
    public async Task<WorkItem?> GetNextPerceptAsync(CancellationToken ct)
    {
        return await queueService.DequeueNextAsync(ct);
    }
}

// ========== POLICY (THINK - with real logic) ==========
public class MathTutoringPolicy : IPolicy<WorkItem, MathAction>
{
    public Task<MathAction> DecideAsync(WorkItem percept, CancellationToken ct)
    {
        // GUARD RAILS: Validate payload exists for actions that need it
        var needsPayload = percept.Type == WorkItemType.SubmitAnswer 
                        || percept.Type == WorkItemType.Explain 
                        || percept.Type == WorkItemType.UploadImage;

        if (needsPayload && string.IsNullOrWhiteSpace(percept.PayloadJson))
        {
            return Task.FromResult(new MathAction(
                percept, 
                MathActionType.RejectInvalid, 
                "Missing required payload"));
        }

        // THINK: Map WorkItemType to MathActionType (could add more logic here)
        var actionType = percept.Type switch
        {
            WorkItemType.NextQuestion => MathActionType.NextQuestion,
            WorkItemType.SubmitAnswer => MathActionType.SubmitAnswer,
            WorkItemType.Explain => MathActionType.Explain,
            WorkItemType.UploadImage => MathActionType.UploadImage,
            _ => MathActionType.RejectInvalid
        };

        return Task.FromResult(new MathAction(percept, actionType));
    }
}

// ========== ACTUATOR ==========
public class MathTutoringActuator : IActuator<MathAction, MathTickResult>
{
    private readonly CurriculumService _curriculumService;
    private readonly AssessmentService _assessmentService;
    private readonly KnowledgeTracingService _knowledgeTracingService;
    private readonly RevisionService _revisionService;
    private readonly ExplanationService _explanationService;
    private readonly ImageIngestionService _imageIngestionService;
    private readonly ValidationService _validationService;
    private readonly ILogger _logger;

    public MathTutoringActuator(
        CurriculumService curriculumService,
        AssessmentService assessmentService,
        KnowledgeTracingService knowledgeTracingService,
        RevisionService revisionService,
        ExplanationService explanationService,
        ImageIngestionService imageIngestionService,
        ValidationService validationService,
        ILogger logger)
    {
        _curriculumService = curriculumService;
        _assessmentService = assessmentService;
        _knowledgeTracingService = knowledgeTracingService;
        _revisionService = revisionService;
        _explanationService = explanationService;
        _imageIngestionService = imageIngestionService;
        _validationService = validationService;
        _logger = logger;
    }

    public async Task<MathTickResult> ExecuteAsync(MathAction action, CancellationToken ct)
    {
        var workItem = action.WorkItem;

        // Handle RejectInvalid from Think layer
        if (action.Type == MathActionType.RejectInvalid)
        {
            _logger.LogWarning("Rejected invalid work item {WorkItemId}: {Reason}", workItem.Id, action.Reason);
            return new MathTickResult
            {
                WorkItemId = workItem.Id,
                Type = workItem.Type,
                StudentId = workItem.StudentId,
                Outcome = TickOutcome.ValidationFailed,
                UiPayload = new { Error = action.Reason }
            };
        }

        return action.Type switch
        {
            MathActionType.NextQuestion => await HandleNextQuestionAsync(workItem, ct),
            MathActionType.SubmitAnswer => await HandleSubmitAnswerAsync(workItem, ct),
            MathActionType.Explain => await HandleExplainAsync(workItem, ct),
            MathActionType.UploadImage => await HandleUploadImageAsync(workItem, ct),
            _ => throw new InvalidOperationException($"Unknown action type: {action.Type}")
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
        SubmitAnswerPayloadDto? payload;
        
        try
        {
            payload = JsonSerializer.Deserialize<SubmitAnswerPayloadDto>(workItem.PayloadJson);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize SubmitAnswer payload for WorkItem {WorkItemId}", workItem.Id);
            return new MathTickResult
            {
                WorkItemId = workItem.Id,
                Type = workItem.Type,
                StudentId = workItem.StudentId,
                Outcome = TickOutcome.ValidationFailed,
                UiPayload = new { Error = "Invalid JSON payload", Details = ex.Message }
            };
        }

        if (payload == null)
        {
            return new MathTickResult
            {
                WorkItemId = workItem.Id,
                Type = workItem.Type,
                StudentId = workItem.StudentId,
                Outcome = TickOutcome.ValidationFailed,
                UiPayload = new { Error = "Payload is null after deserialization" }
            };
        }

        // ✅ VALIDATE INPUT BEFORE PROCESSING
        var (isValid, errors) = await _validationService.ValidateAsync(payload, ct);
        if (!isValid)
        {
            var errorMessage = string.Join("; ", errors);
            _logger.LogWarning("Validation failed for WorkItem {WorkItemId}: {Errors}", workItem.Id, errorMessage);
            
            return new MathTickResult
            {
                WorkItemId = workItem.Id,
                Type = workItem.Type,
                StudentId = workItem.StudentId,
                Outcome = TickOutcome.ValidationFailed,
                UiPayload = new
                {
                    Error = errorMessage,
                    Errors = errors
                }
            };
        }

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
        ExplainPayloadDto? payload;
        
        try
        {
            payload = JsonSerializer.Deserialize<ExplainPayloadDto>(workItem.PayloadJson);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize Explain payload for WorkItem {WorkItemId}", workItem.Id);
            return new MathTickResult
            {
                WorkItemId = workItem.Id,
                Type = workItem.Type,
                StudentId = workItem.StudentId,
                Outcome = TickOutcome.ValidationFailed,
                UiPayload = new { Error = "Invalid JSON payload", Details = ex.Message }
            };
        }

        if (payload == null)
        {
            return new MathTickResult
            {
                WorkItemId = workItem.Id,
                Type = workItem.Type,
                StudentId = workItem.StudentId,
                Outcome = TickOutcome.ValidationFailed,
                UiPayload = new { Error = "Payload is null after deserialization" }
            };
        }

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
        UploadImagePayloadDto? payload;
        
        try
        {
            payload = JsonSerializer.Deserialize<UploadImagePayloadDto>(workItem.PayloadJson);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize UploadImage payload for WorkItem {WorkItemId}", workItem.Id);
            return new MathTickResult
            {
                WorkItemId = workItem.Id,
                Type = workItem.Type,
                StudentId = workItem.StudentId,
                Outcome = TickOutcome.ValidationFailed,
                UiPayload = new { Error = "Invalid JSON payload", Details = ex.Message }
            };
        }

        if (payload == null)
        {
            return new MathTickResult
            {
                WorkItemId = workItem.Id,
                Type = workItem.Type,
                StudentId = workItem.StudentId,
                Outcome = TickOutcome.ValidationFailed,
                UiPayload = new { Error = "Payload is null after deserialization" }
            };
        }

        byte[] imageBytes;
        try
        {
            imageBytes = Convert.FromBase64String(payload.ImageBase64);
        }
        catch (FormatException ex)
        {
            _logger.LogError(ex, "Invalid Base64 string in UploadImage payload for WorkItem {WorkItemId}", workItem.Id);
            return new MathTickResult
            {
                WorkItemId = workItem.Id,
                Type = workItem.Type,
                StudentId = workItem.StudentId,
                Outcome = TickOutcome.ValidationFailed,
                UiPayload = new { Error = "Invalid Base64 image data", Details = ex.Message }
            };
        }

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