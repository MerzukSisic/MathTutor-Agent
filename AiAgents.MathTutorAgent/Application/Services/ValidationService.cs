using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace AiAgents.MathTutorAgent.Application.Services;

public class ValidationService(IServiceProvider serviceProvider)
{
    public async Task<(bool IsValid, string[] Errors)> ValidateAsync<T>(T instance, CancellationToken ct = default)
    {
        var validator = serviceProvider.GetService<IValidator<T>>();
        if (validator == null)
            return (true, Array.Empty<string>());

        var result = await validator.ValidateAsync(instance, ct);
        return (result.IsValid, result.Errors.Select(e => e.ErrorMessage).ToArray());
    }
}