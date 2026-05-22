using EngiFlow.Application.Abstractions.Cqrs;
using EngiFlow.Application.Abstractions.Persistence;
using EngiFlow.Application.Abstractions.Tenancy;
using EngiFlow.Application.Mediation;

namespace EngiFlow.Application.Notifications.Commands;

/// <summary>
/// Command that marks all unread notifications for the current user as read in a single batch.
/// </summary>
public sealed record MarkAllNotificationsAsReadCommand : ICommand<Unit>;

/// <summary>
/// Handles bulk read acknowledgement using optimized database updates.
/// </summary>
public sealed class MarkAllNotificationsAsReadCommandHandler : ICommandHandler<MarkAllNotificationsAsReadCommand, Unit>
{
    private readonly INotificationRepository _notifications;
    private readonly ITenantProvider _tenantProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="MarkAllNotificationsAsReadCommandHandler"/> class.
    /// </summary>
    public MarkAllNotificationsAsReadCommandHandler(INotificationRepository notifications, ITenantProvider tenantProvider)
    {
        _notifications = notifications;
        _tenantProvider = tenantProvider;
    }

    /// <inheritdoc />
    public async Task<Unit> HandleAsync(
        MarkAllNotificationsAsReadCommand command,
        CancellationToken cancellationToken = default)
    {
        await _notifications.MarkAllAsReadAsync(_tenantProvider.CurrentUserId, cancellationToken)
            .ConfigureAwait(false);

        return Unit.Value;
    }
}
