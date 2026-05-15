using EngiFlow.Application.Abstractions.Cqrs;
using Microsoft.Extensions.DependencyInjection;

namespace EngiFlow.Application.Mediation;

/// <summary>
/// EngiFlow-owned mediator that dispatches commands and queries through pipeline behaviors.
/// </summary>
/// <remarks>
/// This mediator intentionally implements only the CQRS capabilities EngiFlow needs:
/// request/response commands, request/response queries, and ordered pipeline behaviors.
/// </remarks>
public sealed class ApplicationMediator : IApplicationMediator
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationMediator"/> class.
    /// </summary>
    /// <param name="serviceProvider">The scoped service provider used to resolve handlers and behaviors.</param>
    public ApplicationMediator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public Task<TResponse> SendCommandAsync<TCommand, TResponse>(
        TCommand command,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand<TResponse>
    {
        ArgumentNullException.ThrowIfNull(command);

        var handler = _serviceProvider.GetRequiredService<ICommandHandler<TCommand, TResponse>>();
        return ExecutePipelineAsync(
            command,
            () => handler.HandleAsync(command, cancellationToken),
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<TResponse> SendQueryAsync<TQuery, TResponse>(
        TQuery query,
        CancellationToken cancellationToken = default)
        where TQuery : IQuery<TResponse>
    {
        ArgumentNullException.ThrowIfNull(query);

        var handler = _serviceProvider.GetRequiredService<IQueryHandler<TQuery, TResponse>>();
        return ExecutePipelineAsync(
            query,
            () => handler.HandleAsync(query, cancellationToken),
            cancellationToken);
    }

    private Task<TResponse> ExecutePipelineAsync<TRequest, TResponse>(
        TRequest request,
        RequestHandlerDelegate<TResponse> handler,
        CancellationToken cancellationToken)
    {
        var behaviors = _serviceProvider
            .GetServices<IPipelineBehavior<TRequest, TResponse>>()
            .Reverse();

        var pipeline = behaviors.Aggregate(
            handler,
            (next, behavior) => () => behavior.HandleAsync(request, next, cancellationToken));

        return pipeline();
    }
}
