namespace EngiFlow.Application.Abstractions.Cqrs;

/// <summary>
/// Handles a state-changing command in the application layer.
/// </summary>
/// <typeparam name="TCommand">The command request type.</typeparam>
/// <typeparam name="TResponse">The response DTO produced by the handler.</typeparam>
public interface ICommandHandler<in TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    /// <summary>
    /// Executes the command use case.
    /// </summary>
    /// <param name="command">The command request to execute.</param>
    /// <param name="cancellationToken">A token that can cancel command execution.</param>
    /// <returns>The command response DTO.</returns>
    Task<TResponse> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}
