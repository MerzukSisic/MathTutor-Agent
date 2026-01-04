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