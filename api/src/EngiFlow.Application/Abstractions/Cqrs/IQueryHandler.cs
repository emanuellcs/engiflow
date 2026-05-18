using EngiFlow.Application.Mediation;

namespace EngiFlow.Application.Abstractions.Cqrs;

/// <summary>
/// Handles a read-only query in the application layer.
/// </summary>
/// <typeparam name="TQuery">The query request type.</typeparam>
/// <typeparam name="TResponse">The response DTO produced by the handler.</typeparam>
public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    /// <summary>
    /// Executes the query use case.
    /// </summary>
    /// <param name="query">The query request to execute.</param>
    /// <param name="cancellationToken">A token that can cancel query execution.</param>
    /// <returns>The query response DTO.</returns>
    Task<TResponse> HandleAsync(TQuery query, CancellationToken cancellationToken = default);

    /// <inheritdoc />
    Task<TResponse> IRequestHandler<TQuery, TResponse>.Handle(
        TQuery request,
        CancellationToken cancellationToken)
    {
        return HandleAsync(request, cancellationToken);
    }
}
