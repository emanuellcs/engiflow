using EngiFlow.Application.Ecos.Notifications;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace EngiFlow.Api.Hubs;

/// <summary>
/// Broadcasts committed ECO application notifications to tenant SignalR groups.
/// </summary>
public sealed class EcoDomainEventHandler : INotificationHandler<EcoChangedNotification>
{
    private readonly IHubContext<EcoHub, IEcoClient> _hubContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="EcoDomainEventHandler"/> class.
    /// </summary>
    /// <param name="hubContext">The ECO SignalR hub context.</param>
    public EcoDomainEventHandler(IHubContext<EcoHub, IEcoClient> hubContext)
    {
        _hubContext = hubContext;
    }

    /// <inheritdoc />
    public async Task Handle(EcoChangedNotification notification, CancellationToken cancellationToken)
    {
        var update = new EcoRealtimeUpdate(
            notification.CompanyId,
            notification.EcoId,
            notification.Status.ToString(),
            notification.ReviewRound,
            notification.Events);

        await _hubContext.Clients
            .Group(EcoHub.TenantGroupName(notification.CompanyId))
            .EcoChanged(update)
            .ConfigureAwait(false);
    }
}
