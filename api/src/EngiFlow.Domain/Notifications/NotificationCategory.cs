namespace EngiFlow.Domain.Notifications;

/// <summary>
/// Defines the priority and context categories for user notifications.
/// </summary>
public enum NotificationCategory
{
    /// <summary>
    /// Indicates a high-priority event requiring direct user intervention or approval.
    /// </summary>
    ActionRequired = 1,

    /// <summary>
    /// Indicates an informational update about a workflow entity the user is following.
    /// </summary>
    Update = 2,

    /// <summary>
    /// Indicates social or commentary activity, such as mentions or new comments.
    /// </summary>
    Activity = 3
}
