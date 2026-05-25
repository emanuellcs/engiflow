using EngiFlow.Application.Abstractions.Messaging;
using EngiFlow.Application.Abstractions.Persistence;
using EngiFlow.Application.Notifications.Dtos;
using EngiFlow.Domain.Notifications;
using EngiFlow.Domain.ValueObjects;
using Microsoft.AspNetCore.SignalR;

namespace EngiFlow.Api.Hubs;

/// <summary>
/// Infrastructure implementation of the notification dispatcher using SignalR.
/// </summary>
public sealed class NotificationService : INotificationService
{
    private readonly IHubContext<NotificationHub, INotificationClient> _hubContext;
    private readonly INotificationRepository _notifications;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationService"/> class.
    /// </summary>
    /// <param name="hubContext">The SignalR hub context.</param>
    /// <param name="notifications">The notification repository.</param>
    /// <param name="unitOfWork">The unit of work.</param>
    public NotificationService(
        IHubContext<NotificationHub, INotificationClient> hubContext,
        INotificationRepository notifications,
        IUnitOfWork unitOfWork)
    {
        _hubContext = hubContext;
        _notifications = notifications;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task NotifyUserAsync(
        UserId userId,
        CompanyId companyId,
        string title,
        string message,
        NotificationCategory category,
        string? deepLink = null,
        CancellationToken cancellationToken = default)
    {
        var notification = Notification.Create(
            userId,
            companyId,
            title,
            message,
            category,
            deepLink);

        await _notifications.AddAsync(notification, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await _hubContext.Clients
            .User(userId.Value.ToString())
            .ReceiveNotification(notification.ToDto())
            .ConfigureAwait(false);
    }
}
