using EngiFlow.Domain.Abstractions;
using EngiFlow.Domain.Guards;
using EngiFlow.Domain.ValueObjects;

namespace EngiFlow.Domain.Ecos;

/// <summary>
/// Immutable audit ledger entry for an engineering change order.
/// </summary>
/// <remarks>
/// ECO events are created by the aggregate when important business changes occur.
/// The private setters exist for persistence tooling, but the domain exposes no
/// mutation methods so audit history remains append-only.
/// </remarks>
public sealed class EcoEvent : ITenantScoped
{
    /// <summary>
    /// Initializes a new empty instance of the <see cref="EcoEvent"/> class for EF Core materialization.
    /// </summary>
    private EcoEvent()
    {
    }

    /// <summary>
    /// Initializes a new immutable audit event.
    /// </summary>
    /// <param name="id">The audit event identifier.</param>
    /// <param name="companyId">The tenant identifier that owns the event.</param>
    /// <param name="engineeringChangeOrderId">The ECO aggregate identifier.</param>
    /// <param name="actorUserId">The user who performed the audited action.</param>
    /// <param name="eventType">The kind of audited business action.</param>
    /// <param name="description">A concise human-readable event description.</param>
    /// <param name="oldStatus">The ECO status before the event, when applicable.</param>
    /// <param name="newStatus">The ECO status after the event, when applicable.</param>
    /// <param name="occurredAt">The UTC timestamp when the action occurred.</param>
    private EcoEvent(
        EcoEventId id,
        CompanyId companyId,
        EngineeringChangeOrderId engineeringChangeOrderId,
        UserId actorUserId,
        EcoEventType eventType,
        string description,
        EcoStatus? oldStatus,
        EcoStatus? newStatus,
        DateTimeOffset occurredAt)
    {
        Id = id;
        CompanyId = companyId;
        EngineeringChangeOrderId = engineeringChangeOrderId;
        ActorUserId = actorUserId;
        EventType = eventType;
        Description = description;
        OldStatus = oldStatus;
        NewStatus = newStatus;
        OccurredAt = occurredAt;
    }

    /// <summary>
    /// Gets the unique audit event identifier.
    /// </summary>
    public EcoEventId Id { get; private set; }

    /// <summary>
    /// Gets the tenant identifier that owns the audit event.
    /// </summary>
    public CompanyId CompanyId { get; private set; }

    /// <summary>
    /// Gets the ECO aggregate that produced the event.
    /// </summary>
    public EngineeringChangeOrderId EngineeringChangeOrderId { get; private set; }

    /// <summary>
    /// Gets the user who performed the audited action.
    /// </summary>
    public UserId ActorUserId { get; private set; }

    /// <summary>
    /// Gets the event classification.
    /// </summary>
    public EcoEventType EventType { get; private set; }

    /// <summary>
    /// Gets a concise description suitable for audit timeline displays.
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the ECO status before the event, when the event represents a status-aware action.
    /// </summary>
    public EcoStatus? OldStatus { get; private set; }

    /// <summary>
    /// Gets the ECO status after the event, when the event represents a status-aware action.
    /// </summary>
    public EcoStatus? NewStatus { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when the audited action occurred.
    /// </summary>
    public DateTimeOffset OccurredAt { get; private set; }

    /// <summary>
    /// Creates a validated immutable audit event.
    /// </summary>
    /// <param name="companyId">The tenant identifier that owns the event.</param>
    /// <param name="engineeringChangeOrderId">The ECO aggregate identifier.</param>
    /// <param name="actorUserId">The user who performed the audited action.</param>
    /// <param name="eventType">The kind of audited business action.</param>
    /// <param name="description">A concise human-readable event description.</param>
    /// <param name="oldStatus">The ECO status before the event, when applicable.</param>
    /// <param name="newStatus">The ECO status after the event, when applicable.</param>
    /// <param name="occurredAt">Optional timestamp used for deterministic tests or imports.</param>
    /// <returns>A validated immutable audit event.</returns>
    internal static EcoEvent Create(
        CompanyId companyId,
        EngineeringChangeOrderId engineeringChangeOrderId,
        UserId actorUserId,
        EcoEventType eventType,
        string description,
        EcoStatus? oldStatus,
        EcoStatus? newStatus,
        DateTimeOffset? occurredAt = null)
    {
        DomainGuard.AgainstDefault(companyId, nameof(companyId));
        DomainGuard.AgainstDefault(engineeringChangeOrderId, nameof(engineeringChangeOrderId));
        DomainGuard.AgainstDefault(actorUserId, nameof(actorUserId));
        DomainGuard.AgainstInvalidEnum(eventType, nameof(eventType));

        if (oldStatus.HasValue)
        {
            DomainGuard.AgainstInvalidEnum(oldStatus.Value, nameof(oldStatus));
        }

        if (newStatus.HasValue)
        {
            DomainGuard.AgainstInvalidEnum(newStatus.Value, nameof(newStatus));
        }

        return new EcoEvent(
            EcoEventId.New(),
            companyId,
            engineeringChangeOrderId,
            actorUserId,
            eventType,
            DomainGuard.Required(description, nameof(description), 500),
            oldStatus,
            newStatus,
            DomainGuard.UtcTimestamp(occurredAt));
    }
}
