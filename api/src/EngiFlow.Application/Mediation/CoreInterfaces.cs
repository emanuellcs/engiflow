namespace EngiFlow.Application.Mediation;

/// <summary>
/// Marker interface for requests that return a response.
/// </summary>
/// <typeparam name="TResponse">The type of response produced by the request.</typeparam>
public interface IRequest<out TResponse> { }

/// <summary>
/// Marker interface for requests that produce a Unit (void-equivalent) response.
/// </summary>
public interface IRequest : IRequest<Unit> { }

/// <summary>
/// Defines a handler for a request.
/// </summary>
/// <typeparam name="TRequest">The type of request being handled.</typeparam>
/// <typeparam name="TResponse">The type of response produced by the handler.</typeparam>
public interface IRequestHandler<in TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// Handles the specified request.
    /// </summary>
    /// <param name="request">The request to handle.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>The response produced by the handler.</returns>
    Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// Marker interface for notifications.
/// </summary>
public interface INotification { }

/// <summary>
/// Defines a handler for a notification.
/// </summary>
/// <typeparam name="TNotification">The type of notification being handled.</typeparam>
public interface INotificationHandler<in TNotification> where TNotification : INotification
{
    /// <summary>
    /// Handles the specified notification.
    /// </summary>
    /// <param name="notification">The notification to handle.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task Handle(TNotification notification, CancellationToken cancellationToken);
}

/// <summary>
/// Represents a delegate for the next handler in the request pipeline.
/// </summary>
/// <typeparam name="TResponse">The type of response produced by the pipeline.</typeparam>
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();

/// <summary>
/// Defines a behavior that wraps request handling in the pipeline.
/// </summary>
/// <typeparam name="TRequest">The type of request being handled.</typeparam>
/// <typeparam name="TResponse">The type of response produced by the pipeline.</typeparam>
public interface IPipelineBehavior<in TRequest, TResponse> where TRequest : notnull
{
    /// <summary>
    /// Handles the request behavior.
    /// </summary>
    /// <param name="request">The request being handled.</param>
    /// <param name="next">The delegate for the next handler in the pipeline.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>The response produced by the pipeline.</returns>
    Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken);
}

/// <summary>
/// Defines a contract for sending requests.
/// </summary>
public interface ISender
{
    /// <summary>
    /// Sends a request to its corresponding handler.
    /// </summary>
    /// <typeparam name="TResponse">The type of response produced by the request.</typeparam>
    /// <param name="request">The request to send.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>The response produced by the handler.</returns>
    Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines a contract for publishing notifications.
/// </summary>
public interface IPublisher
{
    /// <summary>
    /// Publishes a notification to all registered handlers.
    /// </summary>
    /// <param name="notification">The notification to publish.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task Publish(object notification, CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines a combined contract for sending requests and publishing notifications.
/// </summary>
public interface IMediator : ISender, IPublisher { }

/// <summary>
/// Represents a type with a single value, used when a request produces no meaningful response.
/// </summary>
public readonly struct Unit : IEquatable<Unit>
{
    /// <summary>
    /// The single value of the Unit type.
    /// </summary>
    public static readonly Unit Value = new();

    /// <inheritdoc />
    public override int GetHashCode() => 0;

    /// <inheritdoc />
    public bool Equals(Unit other) => true;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Unit;

    /// <summary>
    /// Compares two Unit values for equality.
    /// </summary>
    public static bool operator ==(Unit left, Unit right) => true;

    /// <summary>
    /// Compares two Unit values for inequality.
    /// </summary>
    public static bool operator !=(Unit left, Unit right) => false;
}
