using EngiFlow.Application.Mediation;

namespace EngiFlow.Application.Abstractions.Cqrs;

/// <summary>
/// Marker interface for requests that mutate system state.
/// </summary>
public interface ICommandBase
{
}

/// <summary>
/// Represents an application request that changes system state.
/// </summary>
/// <typeparam name="TResponse">The response DTO produced after the command completes.</typeparam>
public interface ICommand<out TResponse> : IRequest<TResponse>, ICommandBase
{
}
