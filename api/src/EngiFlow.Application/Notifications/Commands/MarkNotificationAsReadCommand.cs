using EngiFlow.Application.Abstractions.Cqrs;
using EngiFlow.Application.Abstractions.Persistence;
using EngiFlow.Application.Abstractions.Tenancy;
using EngiFlow.Application.Exceptions;
using EngiFlow.Application.Mediation;
using EngiFlow.Domain.Notifications;
using EngiFlow.Domain.ValueObjects;

namespace EngiFlow.Application.Notifications.Commands;

/// <summary>
/// Command that marks a specific notification as acknowledged by the user.
/// </summary>
/// <param name="Id">The notification identifier.</param>
public sealed record MarkNotificationAsReadCommand(Guid Id) : ICommand<Unit>;

/// <summary>
/// Handles marking individual notifications as read.
/// </summary>
public sealed class MarkNotificationAsReadCommandHandler : ICommandHandler<MarkNotificationAsReadCommand, Unit>
{
    private readonly INotificationRepository _notifications;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantProvider _tenantProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="MarkNotificationAsReadCommandHandler"/> class.
    /// </summary>
    public MarkNotificationAsReadCommandHandler(
        INotificationRepository notifications,
        IUnitOfWork unitOfWork,
        ITenantProvider tenantProvider)
    {
        _notifications = notifications;
        _unitOfWork = unitOfWork;
        _tenantProvider = tenantProvider;
    }

    /// <inheritdoc />
    public async Task<Unit> HandleAsync(MarkNotificationAsReadCommand command, CancellationToken cancellationToken = default)
    {
        var notification = await _notifications.GetByIdAsync(NotificationId.From(command.Id), cancellationToken)
            .ConfigureAwait(false);

        if (notification is null || notification.UserId != _tenantProvider.CurrentUserId)
        {
            throw new EntityNotFoundException(nameof(Notification), command.Id);
        }

        notification.MarkAsRead();
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}
