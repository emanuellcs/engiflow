using EngiFlow.Domain.Ecos;

namespace EngiFlow.Application.Ecos.Dtos;

/// <summary>
/// Response DTO for an ECO audit timeline entry.
/// </summary>
/// <param name="Id">The audit event identifier.</param>
/// <param name="CompanyId">The tenant identifier that owns the event.</param>
/// <param name="EngineeringChangeOrderId">The ECO identifier that produced the event.</param>
/// <param name="ActorUserId">The user who performed the audited action.</param>
/// <param name="EventType">The audited action type.</param>
/// <param name="Description">A concise audit description.</param>
/// <param name="OldStatus">The ECO status before the event, when applicable.</param>
/// <param name="NewStatus">The ECO status after the event, when applicable.</param>
/// <param name="OccurredAt">The UTC timestamp when the audited action occurred.</param>
public sealed record EcoEventDto(
    Guid Id,
    Guid CompanyId,
    Guid EngineeringChangeOrderId,
    Guid ActorUserId,
    EcoEventType EventType,
    string Description,
    EcoStatus? OldStatus,
    EcoStatus? NewStatus,
    DateTimeOffset OccurredAt);

/// <summary>
/// Detailed ECO response DTO including audit history.
/// </summary>
/// <param name="Id">The ECO identifier.</param>
/// <param name="CompanyId">The tenant identifier that owns the ECO.</param>
/// <param name="Title">The ECO title.</param>
/// <param name="Description">The ECO description.</param>
/// <param name="Priority">The operational priority.</param>
/// <param name="Status">The current lifecycle status.</param>
/// <param name="CreatedByUserId">The user who created the ECO.</param>
/// <param name="CreatedAt">The UTC timestamp when the ECO was created.</param>
/// <param name="UpdatedAt">The UTC timestamp when the ECO was last changed.</param>
/// <param name="Events">The chronological audit timeline for the ECO.</param>
public sealed record EcoDetailsDto(
    Guid Id,
    Guid CompanyId,
    string Title,
    string Description,
    EcoPriority Priority,
    EcoStatus Status,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<EcoEventDto> Events);

/// <summary>
/// Summary ECO response DTO used by paginated list views.
/// </summary>
/// <param name="Id">The ECO identifier.</param>
/// <param name="CompanyId">The tenant identifier that owns the ECO.</param>
/// <param name="Title">The ECO title.</param>
/// <param name="Priority">The operational priority.</param>
/// <param name="Status">The current lifecycle status.</param>
/// <param name="CreatedByUserId">The user who created the ECO.</param>
/// <param name="CreatedAt">The UTC timestamp when the ECO was created.</param>
/// <param name="UpdatedAt">The UTC timestamp when the ECO was last changed.</param>
public sealed record EcoSummaryDto(
    Guid Id,
    Guid CompanyId,
    string Title,
    EcoPriority Priority,
    EcoStatus Status,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
