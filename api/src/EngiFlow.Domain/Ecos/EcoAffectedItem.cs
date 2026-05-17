using EngiFlow.Domain.Abstractions;
using EngiFlow.Domain.Guards;
using EngiFlow.Domain.ValueObjects;

namespace EngiFlow.Domain.Ecos;

/// <summary>
/// Engineering diff row describing a controlled item affected by an ECO.
/// </summary>
public sealed class EcoAffectedItem : ITenantScoped
{
    private EcoAffectedItem()
    {
    }

    private EcoAffectedItem(
        EcoAffectedItemId id,
        CompanyId companyId,
        EngineeringChangeOrderId engineeringChangeOrderId,
        string partNumber,
        string description,
        string currentRevision,
        string newRevision,
        EcoAffectedItemAction action,
        UserId createdByUserId,
        DateTimeOffset createdAt)
    {
        Id = id;
        CompanyId = companyId;
        EngineeringChangeOrderId = engineeringChangeOrderId;
        PartNumber = partNumber;
        Description = description;
        CurrentRevision = currentRevision;
        NewRevision = newRevision;
        Action = action;
        CreatedByUserId = createdByUserId;
        CreatedAt = createdAt;
    }

    /// <summary>
    /// Gets the affected item identifier.
    /// </summary>
    public EcoAffectedItemId Id { get; private set; }

    /// <inheritdoc />
    public CompanyId CompanyId { get; private set; }

    /// <summary>
    /// Gets the ECO identifier this affected item belongs to.
    /// </summary>
    public EngineeringChangeOrderId EngineeringChangeOrderId { get; private set; }

    /// <summary>
    /// Gets the controlled part or artifact number.
    /// </summary>
    public string PartNumber { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the affected item description.
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the current revision before the ECO.
    /// </summary>
    public string CurrentRevision { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the proposed revision after the ECO.
    /// </summary>
    public string NewRevision { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the action this ECO performs on the item.
    /// </summary>
    public EcoAffectedItemAction Action { get; private set; }

    /// <summary>
    /// Gets the user who added the affected item.
    /// </summary>
    public UserId CreatedByUserId { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when the affected item was added.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    internal static EcoAffectedItem Create(
        CompanyId companyId,
        EngineeringChangeOrderId engineeringChangeOrderId,
        string partNumber,
        string description,
        string currentRevision,
        string newRevision,
        EcoAffectedItemAction action,
        UserId createdByUserId,
        DateTimeOffset? createdAt = null)
    {
        DomainGuard.AgainstDefault(companyId, nameof(companyId));
        DomainGuard.AgainstDefault(engineeringChangeOrderId, nameof(engineeringChangeOrderId));
        DomainGuard.AgainstDefault(createdByUserId, nameof(createdByUserId));
        DomainGuard.AgainstInvalidEnum(action, nameof(action));

        return new EcoAffectedItem(
            EcoAffectedItemId.New(),
            companyId,
            engineeringChangeOrderId,
            DomainGuard.Required(partNumber, nameof(partNumber), 100),
            DomainGuard.Required(description, nameof(description), 1_000),
            DomainGuard.Required(currentRevision, nameof(currentRevision), 64),
            DomainGuard.Required(newRevision, nameof(newRevision), 64),
            action,
            createdByUserId,
            DomainGuard.UtcTimestamp(createdAt));
    }
}
