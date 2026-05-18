using FluentValidation;
using EngiFlow.Application.Mediation;
using AppValidationException = EngiFlow.Application.Exceptions.ValidationException;

namespace EngiFlow.Application.Behaviors;

/// <summary>
/// Validates application commands and queries before their handlers execute.
/// </summary>
/// <typeparam name="TRequest">The command or query request type being validated.</typeparam>
/// <typeparam name="TResponse">The response DTO produced by the request pipeline.</typeparam>
/// <remarks>
/// Validators are discovered from the application assembly and run as a pipeline behavior
/// so validation remains consistent whether a use case is invoked from HTTP endpoints,
/// background jobs, or future message consumers.
/// </remarks>
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IReadOnlyCollection<IValidator<TRequest>> _validators;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="validators">The validators that apply to the request type.</param>
    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators.ToArray();
    }

    /// <inheritdoc />
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken = default)
    {
        return await HandleCoreAsync(request, () => next(), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes validation using the legacy in-process CQRS delegate shape.
    /// </summary>
    /// <param name="request">The request being validated.</param>
    /// <param name="next">The legacy next-handler delegate.</param>
    /// <param name="cancellationToken">A token that can cancel validation.</param>
    /// <returns>The response DTO produced by the next handler.</returns>
    public async Task<TResponse> HandleAsync(
        TRequest request,
        EngiFlow.Application.Abstractions.Cqrs.RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken = default)
    {
        return await HandleCoreAsync(request, () => next(), cancellationToken).ConfigureAwait(false);
    }

    private async Task<TResponse> HandleCoreAsync(
        TRequest request,
        Func<Task<TResponse>> next,
        CancellationToken cancellationToken)
    {
        if (_validators.Count == 0)
        {
            return await next().ConfigureAwait(false);
        }

        var context = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(
                _validators.Select(validator => validator.ValidateAsync(context, cancellationToken)))
            .ConfigureAwait(false);

        var failures = validationResults
            .SelectMany(result => result.Errors)
            .Where(failure => failure is not null)
            .GroupBy(failure => failure.PropertyName)
            .ToDictionary(
                group => group.Key,
                group => group.Select(failure => failure.ErrorMessage).Distinct().ToArray());

        if (failures.Count > 0)
        {
            throw new AppValidationException(failures);
        }

        return await next().ConfigureAwait(false);
    }
}
