using Microsoft.Extensions.DependencyInjection;

namespace EngiFlow.Application.Mediation.Internal;

/// <summary>
/// Base class for request handlers to allow non-generic dispatch.
/// </summary>
internal abstract class RequestHandlerBase
{
    /// <summary>
    /// Handles the request using the specified service provider.
    /// </summary>
    public abstract Task<object?> Handle(object request, IServiceProvider serviceProvider, CancellationToken cancellationToken);
}

/// <summary>
/// Typed base class for request handlers.
/// </summary>
internal abstract class RequestHandlerWrapper<TResponse> : RequestHandlerBase
{
    /// <summary>
    /// Handles the typed request.
    /// </summary>
    public abstract Task<TResponse> Handle(IRequest<TResponse> request, IServiceProvider serviceProvider, CancellationToken cancellationToken);
}

/// <summary>
/// Concrete implementation of the request handler wrapper that orchestrates the pipeline.
/// </summary>
internal sealed class RequestHandlerWrapperImpl<TRequest, TResponse> : RequestHandlerWrapper<TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <inheritdoc />
    public override async Task<object?> Handle(object request, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        return await Handle((IRequest<TResponse>)request, serviceProvider, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override Task<TResponse> Handle(IRequest<TResponse> request, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var handler = serviceProvider.GetService<IRequestHandler<TRequest, TResponse>>();
        if (handler == null)
        {
            throw new InvalidOperationException($"Handler was not found for request of type {typeof(TRequest)}.");
        }

        // Resolve behaviors and reverse them so they wrap in the correct order (last registered is outermost)
        // Note: MediatR usually expects them in the order they were registered. 
        // In .NET DI, GetServices returns them in registration order. 
        // To wrap them correctly (first is outermost), we process them from last to first.
        var behaviors = serviceProvider.GetServices<IPipelineBehavior<TRequest, TResponse>>().Reverse().ToList();

        RequestHandlerDelegate<TResponse> next = () => handler.Handle((TRequest)request, cancellationToken);
        
        foreach (var behavior in behaviors)
        {
            var nextBehavior = next;
            next = () => behavior.Handle((TRequest)request, nextBehavior, cancellationToken);
        }

        return next();
    }
}
