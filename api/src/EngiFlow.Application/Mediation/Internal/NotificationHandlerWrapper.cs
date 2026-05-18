using Microsoft.Extensions.DependencyInjection;

namespace EngiFlow.Application.Mediation.Internal;

/// <summary>
/// Base class for notification handlers to allow non-generic dispatch.
/// </summary>
internal abstract class NotificationHandlerWrapper
{
    /// <summary>
    /// Handles the notification using the specified service provider.
    /// </summary>
    public abstract Task Handle(object notification, IServiceProvider serviceProvider, CancellationToken cancellationToken);
}

/// <summary>
/// Concrete implementation of the notification handler wrapper.
/// </summary>
internal sealed class NotificationHandlerWrapperImpl<TNotification> : NotificationHandlerWrapper
    where TNotification : INotification
{
    /// <inheritdoc />
    public override Task Handle(object notification, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var handlers = serviceProvider.GetServices<INotificationHandler<TNotification>>();
        return Task.WhenAll(handlers.Select(h => h.Handle((TNotification)notification, cancellationToken)));
    }
}
