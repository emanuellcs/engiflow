using EngiFlow.Application.Mediation;

namespace EngiFlow.Application.Messaging;

/// <summary>
/// Scoped in-memory post-commit notification queue.
/// </summary>
internal sealed class PostCommitNotificationQueue : IPostCommitNotificationQueue
{
    private readonly List<INotification> _notifications = [];

    /// <inheritdoc />
    public IReadOnlyCollection<INotification> Notifications => _notifications.AsReadOnly();

    /// <inheritdoc />
    public void Enqueue(INotification notification)
    {
        ArgumentNullException.ThrowIfNull(notification);
        _notifications.Add(notification);
    }

    /// <inheritdoc />
    public void Clear()
    {
        _notifications.Clear();
    }
}
