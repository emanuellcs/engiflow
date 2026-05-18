using System.Collections.Concurrent;
using EngiFlow.Application.Mediation.Internal;

namespace EngiFlow.Application.Mediation;

/// <summary>
/// Default implementation of the IMediator interface.
/// </summary>
public sealed class Mediator : IMediator
{
    private readonly IServiceProvider _serviceProvider;
    private static readonly ConcurrentDictionary<Type, RequestHandlerBase> _requestHandlers = new();
    private static readonly ConcurrentDictionary<Type, NotificationHandlerWrapper> _notificationHandlers = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="Mediator"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider used to resolve handlers and behaviors.</param>
    public Mediator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        
        var requestType = request.GetType();
        var handler = (RequestHandlerWrapper<TResponse>)_requestHandlers.GetOrAdd(requestType, static (t, responseType) =>
        {
            var wrapperType = typeof(RequestHandlerWrapperImpl<,>).MakeGenericType(t, responseType);
            return (RequestHandlerBase)Activator.CreateInstance(wrapperType)!;
        }, typeof(TResponse));

        return handler.Handle(request, _serviceProvider, cancellationToken);
    }

    /// <inheritdoc />
    public Task Publish(object notification, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);
        
        var notificationType = notification.GetType();
        var handler = _notificationHandlers.GetOrAdd(notificationType, static t =>
        {
            var wrapperType = typeof(NotificationHandlerWrapperImpl<>).MakeGenericType(t);
            return (NotificationHandlerWrapper)Activator.CreateInstance(wrapperType)!;
        });

        return handler.Handle(notification, _serviceProvider, cancellationToken);
    }
}
