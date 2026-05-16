using System.Text.Json;
using AiAgents.Core;
using AiAgents.MathTutorAgent.Application.DTOs;
using AiAgents.MathTutorAgent.Application.Services;
using AiAgents.MathTutorAgent.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace AiAgents.MathTutorAgent.Application.Runners;

public class MathTutoringActuator(
    CurriculumService curriculumService,
    AssessmentService assessmentService,
    KnowledgeTracingService knowledgeTracingService,
    CrossMathMilestoneService crossMathMilestoneService,
    RevisionService revisionService,
    ExplanationService explanationService,
    MathContentLocalizationService localizationService,
    ImageIngestionService imageIngestionService,
    ValidationService validationService,
    ILogger logger)
    : IActuator<MathAction, MathTickResult>
{
    public async Task<MathTickResult> ExecuteAsync(MathAction action, CancellationToken ct)
    {
        var workItem = action.WorkItem;

        // Handle RejectInvalid from Think layer
        if (action.Type == MathActionType.RejectInvalid)
        {
            logger.LogWarning("Rejected invalid work item {WorkItemId}: {Reason}", workItem.Id, action.Reason);
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
        NextQuestionPayloadDto? nextPayload = null;
        try
        {
            nextPayload = JsonSerializer.Deserialize<NextQuestionPayloadDto>(workItem.PayloadJson);
        }
        catch (JsonException)
        {
            // optional payload - ignore and fallback to BS
        }

        var language = localizationService.NormalizeLanguage(nextPayload?.Language);

        var needsRevision = await revisionService.ShouldInjectRevisionAsync(workItem.StudentId, ct);
        int? topicId = null;

        if (needsRevision)
        {
            var revisionTopic = await revisionService.PickRevisionTopicAsync(workItem.StudentId, ct);
            topicId = revisionTopic?.Id;
        }

        if (topicId == null)
        {
            var nextTopic = await curriculumService.GetNextTopicAsync(workItem.StudentId, ct);
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

        var question = await assessmentService.SelectNextQuestionAsync(workItem.StudentId, topicId.Value, ct);
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

        // Ensure Topic navigation is loaded for UI metadata (area/topic labels).
        question = await assessmentService.GetQuestionAsync(question.Id, ct) ?? question;

        await revisionService.UpdateScheduleAsync(workItem.StudentId, topicId.Value, ct);
        var isFirstInTopic = await assessmentService.IsFirstQuestionInTopicForStudentAsync(
            workItem.StudentId,
            topicId.Value,
            ct);
        var timeLimitSeconds = await assessmentService.GetTimeLimitSecondsAsync(workItem.StudentId, question, ct);
        var localizedQuestionText = localizationService.LocalizeQuestionText(question.QuestionText, language);
        var localizedTopicName = localizationService.LocalizeTopicName(question.Topic?.Name ?? string.Empty, language);
        var localizedAreaName = localizationService.LocalizeAreaName(question.Topic?.Area.ToString() ?? string.Empty, language);

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
                TopicName = localizedTopicName,
                AreaName = localizedAreaName,
                QuestionText = localizedQuestionText,
                Difficulty = question.Difficulty,
                TimeLimitSeconds = timeLimitSeconds,
                IsFirstQuestionInTopicForStudent = isFirstInTopic
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
            logger.LogError(ex, "Failed to deserialize SubmitAnswer payload for WorkItem {WorkItemId}", workItem.Id);
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
        var (isValid, errors) = await validationService.ValidateAsync(payload, ct);
        if (!isValid)
        {
            var errorMessage = string.Join("; ", errors);
            logger.LogWarning("Validation failed for WorkItem {WorkItemId}: {Errors}", workItem.Id, errorMessage);
            
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

        var question = await assessmentService.GetQuestionAsync(payload.QuestionId, ct);
        if (question == null)
            throw new InvalidOperationException($"Question {payload.QuestionId} not found");

        var timeLimitSeconds = await assessmentService.GetTimeLimitSecondsAsync(workItem.StudentId, question, ct);
        var isTimedOut = payload.TimedOut || payload.TimeMs > timeLimitSeconds * 1000;
        var isCorrect = !isTimedOut && await assessmentService.EvaluateAnswerAsync(question, payload.Answer, ct);
        var language = localizationService.NormalizeLanguage(payload.Language);
        var tags = new List<string>();
        if (isTimedOut)
        {
            tags.Add("TIMEOUT");
        }
        
        var attempt = await assessmentService.SaveAttemptAsync(
            workItem.StudentId,
            payload.QuestionId,
            isCorrect,
            payload.TimeMs,
            payload.Answer,
            tags,
            ct);

        var previousMastery = await knowledgeTracingService.GetMasteryScoreAsync(workItem.StudentId, question.TopicId, ct);
        await knowledgeTracingService.UpdateTopicStateAsync(workItem.StudentId, attempt, ct);
        var newMastery = await knowledgeTracingService.GetMasteryScoreAsync(workItem.StudentId, question.TopicId, ct);

        var decision = await assessmentService.DetermineAdvanceDecisionAsync(workItem.StudentId, question.TopicId, ct);
        var milestoneChallenge = await crossMathMilestoneService.GetChallengeToStartAfterAnswerAsync(
            workItem.StudentId,
            question.TopicId,
            newMastery,
            ct);
        milestoneChallenge = localizationService.LocalizeMilestone(milestoneChallenge, language);

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
                IsTimedOut = isTimedOut,
                CorrectAnswer = localizationService.LocalizeAnswerToken(question.CorrectAnswer, language),
                MasteryScore = newMastery,
                Decision = localizationService.LocalizeDecision(decision.ToString(), language),
                TimeLimitSeconds = timeLimitSeconds,
                TimeSpentSeconds = Math.Round(payload.TimeMs / 1000.0, 1),
                MilestoneChallenge = milestoneChallenge
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
            logger.LogError(ex, "Failed to deserialize Explain payload for WorkItem {WorkItemId}", workItem.Id);
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

        var explanation = await explanationService.RetrieveAndComposeExplanationAsync(
            workItem.StudentId,
            payload.QuestionId,
            payload.TopicId,
            payload.ErrorTag,
            payload.Language,
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
            logger.LogError(ex, "Failed to deserialize UploadImage payload for WorkItem {WorkItemId}", workItem.Id);
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
            logger.LogError(ex, "Invalid Base64 string in UploadImage payload for WorkItem {WorkItemId}", workItem.Id);
            return new MathTickResult
            {
                WorkItemId = workItem.Id,
                Type = workItem.Type,
                StudentId = workItem.StudentId,
                Outcome = TickOutcome.ValidationFailed,
                UiPayload = new { Error = "Invalid Base64 image data", Details = ex.Message }
            };
        }

        var imageNote = await imageIngestionService.IngestImageAsync(
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
