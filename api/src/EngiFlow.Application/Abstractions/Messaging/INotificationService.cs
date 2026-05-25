using EngiFlow.Domain.Notifications;
using EngiFlow.Domain.ValueObjects;

namespace EngiFlow.Application.Abstractions.Messaging;

/// <summary>
/// Defines a service for creating and dispatching user notifications in real-time.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Generates a notification, persists it, and streams it to the recipient's active connections.
    /// </summary>
    /// <param name="userId">The recipient user identifier.</param>
    /// <param name="companyId">The tenant identifier.</param>
    /// <param name="title">The notification title.</param>
    /// <param name="message">The notification body.</param>
    /// <param name="category">The notification category.</param>
    /// <param name="deepLink">Optional navigation target.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    Task NotifyUserAsync(
        UserId userId,
        CompanyId companyId,
        string title,
        string message,
        NotificationCategory category,
        string? deepLink = null,
        CancellationToken cancellationToken = default);
}
