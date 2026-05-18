using EngiFlow.Domain.Users;
using EngiFlow.Application.Messaging;
using MediatR;

namespace EngiFlow.Application.Users.Notifications;

/// <summary>
/// Notification published after a tenant user's role changes.
/// </summary>
/// <param name="UserId">The affected user identifier.</param>
/// <param name="NewRole">The user's new role.</param>
public sealed record UserPermissionsChangedNotification(
    Guid UserId,
    UserRole NewRole) : INotification;

/// <summary>
/// Notification published after a tenant user is deactivated.
/// </summary>
/// <param name="UserId">The affected user identifier.</param>
public sealed record UserDeactivatedNotification(Guid UserId) : INotification;

/// <summary>
/// Queues user security notifications from application command handlers.
/// </summary>
internal static class UserSecurityNotificationQueueExtensions
{
    /// <summary>
    /// Queues a role-change notification for post-commit publication.
    /// </summary>
    public static void EnqueueUserPermissionsChanged(
        this IPostCommitNotificationQueue queue,
        User user)
    {
        queue.Enqueue(new UserPermissionsChangedNotification(user.Id.Value, user.Role));
    }

    /// <summary>
    /// Queues a deactivation notification for post-commit publication.
    /// </summary>
    public static void EnqueueUserDeactivated(
        this IPostCommitNotificationQueue queue,
        User user)
    {
        queue.Enqueue(new UserDeactivatedNotification(user.Id.Value));
    }
}
