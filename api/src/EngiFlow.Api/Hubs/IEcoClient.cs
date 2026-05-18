using EngiFlow.Application.Ecos.Dtos;

namespace EngiFlow.Api.Hubs;

/// <summary>
/// Client contract for ECO real-time SignalR messages.
/// </summary>
public interface IEcoClient
{
    /// <summary>
    /// Receives a tenant-scoped ECO timeline or status update.
    /// </summary>
    /// <param name="update">The ECO update payload.</param>
    Task EcoChanged(EcoRealtimeUpdate update);
}

/// <summary>
/// SignalR payload emitted after an ECO command commits.
/// </summary>
/// <param name="CompanyId">The tenant identifier.</param>
/// <param name="EcoId">The ECO identifier.</param>
/// <param name="Status">The current ECO status.</param>
/// <param name="ReviewRound">The current review round.</param>
/// <param name="Events">The current ECO timeline events.</param>
public sealed record EcoRealtimeUpdate(
    Guid CompanyId,
    Guid EcoId,
    string Status,
    int ReviewRound,
    IReadOnlyCollection<EcoEventDto> Events);
