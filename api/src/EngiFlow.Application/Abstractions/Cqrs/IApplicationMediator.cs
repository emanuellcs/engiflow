namespace EngiFlow.Application.Abstractions.Cqrs;

/// <summary>
/// Dispatches application commands and queries through the configured handler and pipeline behaviors.
/// </summary>
/// <remarks>
/// The mediator is intentionally owned by EngiFlow so the application layer can keep a
/// small CQRS surface without coupling use cases to a third-party mediator package.
/// </remarks>
public interface IApplicationMediator
{
    /// <summary>
    /// Sends a state-changing command to its registered command handler.
    /// </summary>
    /// <typeparam name="TCommand">The command request type.</typeparam>
    /// <typeparam name="TResponse">The response DTO produced by the command.</typeparam>
    /// <param name="command">The command request to execute.</param>
    /// <param name="cancellationToken">A token that can cancel the command execution.</param>
    /// <returns>The command response DTO.</returns>
    Task<TResponse> SendCommandAsync<TCommand, TResponse>(
        TCommand command,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand<TResponse>;

    /// <summary>
    /// Sends a read-only query to its registered query handler.
    /// </summary>
    /// <typeparam name="TQuery">The query request type.</typeparam>
    /// <typeparam name="TResponse">The response DTO produced by the query.</typeparam>
    /// <param name="query">The query request to execute.</param>
    /// <param name="cancellationToken">A token that can cancel the query execution.</param>
    /// <returns>The query response DTO.</returns>
    Task<TResponse> SendQueryAsync<TQuery, TResponse>(
        TQuery query,
        CancellationToken cancellationToken = default)
        where TQuery : IQuery<TResponse>;
}
