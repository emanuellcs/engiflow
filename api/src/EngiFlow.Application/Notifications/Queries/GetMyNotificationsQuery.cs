using EngiFlow.Application.Abstractions.Cqrs;
using EngiFlow.Application.Abstractions.Persistence;
using EngiFlow.Application.Abstractions.Tenancy;
using EngiFlow.Application.Notifications.Dtos;

namespace EngiFlow.Application.Notifications.Queries;

/// <summary>
/// Query that fetches the most recent notifications for the currently authenticated user.
/// </summary>
public sealed record GetMyNotificationsQuery : IQuery<IReadOnlyList<NotificationDto>>;

/// <summary>
/// Handles retrieving notifications with strict TOP 20 performance limits.
/// </summary>
public sealed class GetMyNotificationsQueryHandler : IQueryHandler<GetMyNotificationsQuery, IReadOnlyList<NotificationDto>>
{
    private readonly INotificationRepository _notifications;
    private readonly ITenantProvider _tenantProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetMyNotificationsQueryHandler"/> class.
    /// </summary>
    /// <param name="notifications">The notification repository.</param>
    /// <param name="tenantProvider">The current tenant provider.</param>
    public GetMyNotificationsQueryHandler(INotificationRepository notifications, ITenantProvider tenantProvider)
    {
        _notifications = notifications;
        _tenantProvider = tenantProvider;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<NotificationDto>> HandleAsync(
        GetMyNotificationsQuery query,
        CancellationToken cancellationToken = default)
    {
        var recent = await _notifications.ListRecentAsync(_tenantProvider.CurrentUserId, cancellationToken)
            .ConfigureAwait(false);

        return recent.ToDtoList();
    }
}
