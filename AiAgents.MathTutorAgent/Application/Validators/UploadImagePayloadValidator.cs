using AiAgents.MathTutorAgent.Application.DTOs;
using FluentValidation;

namespace AiAgents.MathTutorAgent.Application.Validators;

public class UploadImagePayloadValidator : AbstractValidator<UploadImagePayloadDto>
{
    private static readonly string[] AllowedExtensions = [".png", ".jpg", ".jpeg", ".webp", ".gif"];
    private const int MaxBase64Length = 7_000_000;

    public UploadImagePayloadValidator()
    {
        RuleFor(x => x.ImageBase64)
            .NotEmpty().WithMessage("Image content is required")
            .Must(x => x.Length <= MaxBase64Length).WithMessage("Image payload is too large");

        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("File name is required")
            .Must(HasAllowedExtension).WithMessage("Unsupported image extension");
    }

    private static bool HasAllowedExtension(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(extension))
        {
            return false;
        }

        return AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
    }
}
