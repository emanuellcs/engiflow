using EngiFlow.Application.Users.Notifications;
using EngiFlow.Application.Mediation;
using Microsoft.AspNetCore.SignalR;

namespace EngiFlow.Api.Hubs;

/// <summary>
/// Broadcasts committed user-security notifications to affected SignalR users.
/// </summary>
public sealed class UserSecurityNotificationHandler :
    INotificationHandler<UserPermissionsChangedNotification>,
    INotificationHandler<UserDeactivatedNotification>
{
    private readonly IHubContext<SecurityHub, ISecurityClient> _hubContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserSecurityNotificationHandler"/> class.
    /// </summary>
    public UserSecurityNotificationHandler(IHubContext<SecurityHub, ISecurityClient> hubContext)
    {
        _hubContext = hubContext;
    }

    /// <inheritdoc />
    public async Task Handle(
        UserPermissionsChangedNotification notification,
        CancellationToken cancellationToken)
    {
        await _hubContext.Clients
            .User(notification.UserId.ToString("D"))
            .UserPermissionsChanged(notification.UserId, notification.NewRole.ToString())
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task Handle(
        UserDeactivatedNotification notification,
        CancellationToken cancellationToken)
    {
        await _hubContext.Clients
            .User(notification.UserId.ToString("D"))
            .UserDeactivated(notification.UserId)
            .ConfigureAwait(false);
    }
}
