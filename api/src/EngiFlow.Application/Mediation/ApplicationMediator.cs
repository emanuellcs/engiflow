using EngiFlow.Application.Abstractions.Cqrs;
using MediatR;

namespace EngiFlow.Application.Mediation;

/// <summary>
/// EngiFlow-owned mediator facade backed by MediatR.
/// </summary>
/// <remarks>
/// Controllers keep depending on the EngiFlow abstraction while MediatR owns handler
/// dispatch, ordered pipeline behaviors, and notification publishing internally.
/// </remarks>
public sealed class ApplicationMediator : IApplicationMediator
{
    private readonly ISender _sender;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationMediator"/> class.
    /// </summary>
    /// <param name="sender">The MediatR sender used to dispatch handlers.</param>
    public ApplicationMediator(ISender sender)
    {
        _sender = sender;
    }

    /// <inheritdoc />
    public Task<TResponse> SendCommandAsync<TCommand, TResponse>(
        TCommand command,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand<TResponse>
    {
        ArgumentNullException.ThrowIfNull(command);
        return _sender.Send(command, cancellationToken);
    }

    /// <inheritdoc />
    public Task<TResponse> SendQueryAsync<TQuery, TResponse>(
        TQuery query,
        CancellationToken cancellationToken = default)
        where TQuery : IQuery<TResponse>
    {
        ArgumentNullException.ThrowIfNull(query);
        return _sender.Send(query, cancellationToken);
    }
}
