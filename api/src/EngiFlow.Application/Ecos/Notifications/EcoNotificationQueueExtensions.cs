using EngiFlow.Application.Ecos.Dtos;
using EngiFlow.Application.Messaging;
using EngiFlow.Domain.Ecos;

namespace EngiFlow.Application.Ecos.Notifications;

/// <summary>
/// Queues ECO real-time notifications from application command handlers.
/// </summary>
internal static class EcoNotificationQueueExtensions
{
    /// <summary>
    /// Queues an ECO changed notification for post-commit publication.
    /// </summary>
    /// <param name="queue">The post-commit queue.</param>
    /// <param name="eco">The committed ECO aggregate.</param>
    public static void EnqueueEcoChanged(this IPostCommitNotificationQueue queue, EngineeringChangeOrder eco)
    {
        var details = eco.ToDetailsDto();
        queue.Enqueue(new EcoChangedNotification(
            details.CompanyId,
            details.Id,
            details.Status,
            details.ReviewRound,
            details.Events));
    }
}
