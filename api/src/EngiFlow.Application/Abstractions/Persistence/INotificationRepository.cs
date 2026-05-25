using EngiFlow.Domain.Notifications;
using EngiFlow.Domain.ValueObjects;

namespace EngiFlow.Application.Abstractions.Persistence;

/// <summary>
/// Provides tenant-scoped persistence operations for user notifications.
/// </summary>
public interface INotificationRepository
{
    /// <summary>
    /// Adds a new notification to the persistence store.
    /// </summary>
    /// <param name="notification">The notification to persist.</param>
    /// <param name="cancellationToken">A token that can cancel the persistence operation.</param>
    Task AddAsync(Notification notification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a single notification by its identifier.
    /// </summary>
    /// <param name="id">The notification identifier.</param>
    /// <param name="cancellationToken">A token that can cancel the request.</param>
    /// <returns>The notification when found; otherwise, <see langword="null"/>.</returns>
    Task<Notification?> GetByIdAsync(NotificationId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists the most recent notifications for the currently authenticated user in the current tenant.
    /// </summary>
    /// <remarks>
    /// This query is strictly capped at 20 items to avoid overloading the memory and network.
    /// </remarks>
    /// <param name="userId">The recipient user identifier.</param>
    /// <param name="cancellationToken">A token that can cancel the request.</param>
    /// <returns>A list of up to 20 recent notifications.</returns>
    Task<IReadOnlyList<Notification>> ListRecentAsync(UserId userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks all unread notifications for the user as read using an optimized batch update.
    /// </summary>
    /// <param name="userId">The recipient user identifier.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>The number of notifications marked as read.</returns>
    Task<int> MarkAllAsReadAsync(UserId userId, CancellationToken cancellationToken = default);
}
