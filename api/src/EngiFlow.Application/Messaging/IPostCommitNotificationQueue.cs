using MediatR;

namespace EngiFlow.Application.Messaging;

/// <summary>
/// Queues MediatR notifications that must be published only after a database commit succeeds.
/// </summary>
public interface IPostCommitNotificationQueue
{
    /// <summary>
    /// Gets the queued notifications.
    /// </summary>
    IReadOnlyCollection<INotification> Notifications { get; }

    /// <summary>
    /// Queues a notification for post-commit publishing.
    /// </summary>
    /// <param name="notification">The notification to publish after commit.</param>
    void Enqueue(INotification notification);

    /// <summary>
    /// Clears all queued notifications.
    /// </summary>
    void Clear();
}
