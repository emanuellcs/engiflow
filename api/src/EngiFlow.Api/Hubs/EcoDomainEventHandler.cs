using EngiFlow.Application.Abstractions.Messaging;
using EngiFlow.Application.Ecos.Dtos;
using EngiFlow.Application.Ecos.Notifications;
using EngiFlow.Application.Mediation;
using EngiFlow.Domain.Ecos;
using EngiFlow.Domain.Notifications;
using EngiFlow.Domain.ValueObjects;
using Microsoft.AspNetCore.SignalR;

namespace EngiFlow.Api.Hubs;

/// <summary>
/// Broadcasts committed ECO application notifications to tenant SignalR groups and
/// dispatches targeted personal alerts to relevant users.
/// </summary>
public sealed class EcoDomainEventHandler : INotificationHandler<EcoChangedNotification>
{
    private readonly IHubContext<EcoHub, IEcoClient> _hubContext;
    private readonly INotificationService _notificationService;

    /// <summary>
    /// Initializes a new instance of the <see cref="EcoDomainEventHandler"/> class.
    /// </summary>
    /// <param name="hubContext">The ECO SignalR hub context.</param>
    /// <param name="notificationService">The personal notification dispatcher service.</param>
    public EcoDomainEventHandler(
        IHubContext<EcoHub, IEcoClient> hubContext,
        INotificationService notificationService)
    {
        _hubContext = hubContext;
        _notificationService = notificationService;
    }

    /// <inheritdoc />
    public async Task Handle(EcoChangedNotification notification, CancellationToken cancellationToken)
    {
        // 1. Broadcast the generic update to the workspace hub (SignalR Group)
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

        // 2. Dispatch personal alerts to the ECO owner if the action was performed by someone else
        var latestEvent = notification.Events.MaxBy(e => e.OccurredAt);
        if (latestEvent is null || latestEvent.ActorUserId == notification.CreatedByUserId)
        {
            return;
        }

        var (title, message, category) = GetNotificationDetails(notification, latestEvent);
        if (title is null)
        {
            return;
        }

        await _notificationService.NotifyUserAsync(
                UserId.From(notification.CreatedByUserId),
                CompanyId.From(notification.CompanyId),
                title,
                message!,
                category,
                $"/ecos/{notification.EcoId}",
                cancellationToken)
            .ConfigureAwait(false);
    }

    private static (string? Title, string? Message, NotificationCategory Category) GetNotificationDetails(
        EcoChangedNotification notification,
        EcoEventDto latestEvent)
    {
        return latestEvent.EventType switch
        {
            EcoEventType.CommentAdded => (
                $"New Comment on {notification.Title}",
                "A reviewer or teammate added a new comment to your ECO timeline.",
                NotificationCategory.Activity),

            EcoEventType.SubmittedForReview => (
                $"{notification.Title} Submitted for Review",
                "An ECO has been moved into the formal review phase and is awaiting approval.",
                NotificationCategory.Update),

            EcoEventType.ChangesRequested => (
                $"Changes Requested for {notification.Title}",
                "An approver has requested changes. Review their feedback and resubmit when ready.",
                NotificationCategory.ActionRequired),

            EcoEventType.Approved => (
                $"{notification.Title} Approved",
                "Your ECO has reached quorum and has been formally approved.",
                NotificationCategory.Update),

            EcoEventType.ReviewDecisionSubmitted => (
                $"New Review Decision on {notification.Title}",
                "A team member has submitted their decision for the current review round.",
                NotificationCategory.Update),

            _ => (null, null, NotificationCategory.Update)
        };
    }
}
