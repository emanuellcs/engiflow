using MediatR;

namespace EngiFlow.Application.Abstractions.Cqrs;

/// <summary>
/// Represents an application request that retrieves data without changing system state.
/// </summary>
/// <typeparam name="TResponse">The response DTO produced by the query.</typeparam>
public interface IQuery<out TResponse> : IRequest<TResponse>
{
}
