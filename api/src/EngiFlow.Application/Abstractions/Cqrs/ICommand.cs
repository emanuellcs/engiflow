namespace EngiFlow.Application.Abstractions.Cqrs;

/// <summary>
/// Represents an application request that changes system state.
/// </summary>
/// <typeparam name="TResponse">The response DTO produced after the command completes.</typeparam>
public interface ICommand<out TResponse>
{
}
