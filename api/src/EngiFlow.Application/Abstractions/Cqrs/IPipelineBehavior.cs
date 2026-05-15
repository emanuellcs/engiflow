namespace EngiFlow.Application.Abstractions.Cqrs;

/// <summary>
/// Represents the next handler delegate in an application request pipeline.
/// </summary>
/// <typeparam name="TResponse">The response DTO produced by the pipeline.</typeparam>
/// <returns>The response DTO produced by the next behavior or final handler.</returns>
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();

/// <summary>
/// Adds cross-cutting behavior around application command and query handling.
/// </summary>
/// <typeparam name="TRequest">The command or query request type.</typeparam>
/// <typeparam name="TResponse">The response DTO produced by the request.</typeparam>
public interface IPipelineBehavior<in TRequest, TResponse>
{
    /// <summary>
    /// Executes behavior before or after the next pipeline component.
    /// </summary>
    /// <param name="request">The command or query request being handled.</param>
    /// <param name="next">The next pipeline component.</param>
    /// <param name="cancellationToken">A token that can cancel request processing.</param>
    /// <returns>The response DTO produced by the request pipeline.</returns>
    Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken = default);
}
