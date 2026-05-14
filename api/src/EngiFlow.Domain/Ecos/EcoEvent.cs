using EngiFlow.Domain.Abstractions;
using EngiFlow.Domain.Guards;
using EngiFlow.Domain.ValueObjects;

namespace EngiFlow.Domain.Ecos;

/// <summary>
/// Immutable audit ledger entry for an engineering change order.
/// </summary>
public sealed class EcoEvent : ITenantScoped
{
    private EcoEvent()
    {
    }

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

    public EcoEventId Id { get; private set; }

    public CompanyId CompanyId { get; private set; }

    public EngineeringChangeOrderId EngineeringChangeOrderId { get; private set; }

    public UserId ActorUserId { get; private set; }

    public EcoEventType EventType { get; private set; }

    public string Description { get; private set; } = string.Empty;

    public EcoStatus? OldStatus { get; private set; }

    public EcoStatus? NewStatus { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

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
