using EngiFlow.Domain.Ecos;

namespace EngiFlow.Application.Ecos.Dtos;

/// <summary>
/// Maps ECO domain aggregates and audit events into application DTOs.
/// </summary>
internal static class EcoMappingExtensions
{
    /// <summary>
    /// Converts an ECO aggregate into a detailed response DTO with sorted audit history.
    /// </summary>
    /// <param name="eco">The ECO aggregate to map.</param>
    /// <returns>A detailed response DTO.</returns>
    public static EcoDetailsDto ToDetailsDto(this EngineeringChangeOrder eco)
    {
        return new EcoDetailsDto(
            eco.Id.Value,
            eco.CompanyId.Value,
            eco.Title,
            eco.Description,
            eco.Priority,
            eco.Status,
            eco.CreatedByUserId.Value,
            eco.CreatedAt,
            eco.UpdatedAt,
            eco.Events
                .OrderBy(ecoEvent => ecoEvent.OccurredAt)
                .ThenBy(ecoEvent => ecoEvent.Id.Value)
                .Select(ToEventDto)
                .ToArray());
    }

    /// <summary>
    /// Converts an ECO aggregate into a summary response DTO.
    /// </summary>
    /// <param name="eco">The ECO aggregate to map.</param>
    /// <returns>A summary response DTO.</returns>
    public static EcoSummaryDto ToSummaryDto(this EngineeringChangeOrder eco)
    {
        return new EcoSummaryDto(
            eco.Id.Value,
            eco.CompanyId.Value,
            eco.Title,
            eco.Priority,
            eco.Status,
            eco.CreatedByUserId.Value,
            eco.CreatedAt,
            eco.UpdatedAt);
    }

    private static EcoEventDto ToEventDto(EcoEvent ecoEvent)
    {
        return new EcoEventDto(
            ecoEvent.Id.Value,
            ecoEvent.CompanyId.Value,
            ecoEvent.EngineeringChangeOrderId.Value,
            ecoEvent.ActorUserId.Value,
            ecoEvent.EventType,
            ecoEvent.Description,
            ecoEvent.OldStatus,
            ecoEvent.NewStatus,
            ecoEvent.OccurredAt);
    }
}
