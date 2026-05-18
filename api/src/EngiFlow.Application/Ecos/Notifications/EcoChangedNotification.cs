using EngiFlow.Application.Ecos.Dtos;
using EngiFlow.Domain.Ecos;
using EngiFlow.Application.Mediation;

namespace EngiFlow.Application.Ecos.Notifications;

/// <summary>
/// Notification published after an ECO command commits successfully.
/// </summary>
/// <param name="CompanyId">The tenant identifier to broadcast to.</param>
/// <param name="EcoId">The changed ECO identifier.</param>
/// <param name="Status">The ECO status after the committed command.</param>
/// <param name="ReviewRound">The ECO review round after the committed command.</param>
/// <param name="Events">The current ordered event timeline.</param>
public sealed record EcoChangedNotification(
    Guid CompanyId,
    Guid EcoId,
    EcoStatus Status,
    int ReviewRound,
    IReadOnlyList<EcoEventDto> Events) : INotification;
