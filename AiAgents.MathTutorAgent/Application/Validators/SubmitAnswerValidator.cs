using AiAgents.MathTutorAgent.Application.DTOs;
using AiAgents.MathTutorAgent.Application.Runners;
using FluentValidation;

namespace AiAgents.MathTutorAgent.Application.Validators;

public class SubmitAnswerValidator : AbstractValidator<SubmitAnswerPayloadDto>
{
    public SubmitAnswerValidator()
    {
        RuleFor(x => x.QuestionId).GreaterThan(0).WithMessage("Question ID must be positive");
        RuleFor(x => x.Answer).NotEmpty().WithMessage("Answer cannot be empty");
        RuleFor(x => x.TimeMs).GreaterThan(0).WithMessage("Time must be positive");
    }
}