using EngiFlow.Domain.Abstractions;
using EngiFlow.Domain.Exceptions;
using EngiFlow.Domain.Guards;
using EngiFlow.Domain.ValueObjects;

namespace EngiFlow.Domain.Ecos;

/// <summary>
/// Aggregate root for the formal ECO approval workflow.
/// </summary>
public sealed class EngineeringChangeOrder : ITenantScoped
{
    private readonly List<EcoEvent> _events = [];

    private EngineeringChangeOrder()
    {
    }

    private EngineeringChangeOrder(
        EngineeringChangeOrderId id,
        CompanyId companyId,
        string title,
        string description,
        EcoPriority priority,
        UserId createdByUserId,
        DateTimeOffset createdAt)
    {
        Id = id;
        CompanyId = companyId;
        Title = title;
        Description = description;
        Priority = priority;
        Status = EcoStatus.Draft;
        CreatedByUserId = createdByUserId;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public EngineeringChangeOrderId Id { get; private set; }

    public CompanyId CompanyId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public EcoPriority Priority { get; private set; }

    public EcoStatus Status { get; private set; }

    public UserId CreatedByUserId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyCollection<EcoEvent> Events => _events.AsReadOnly();

    public static EngineeringChangeOrder Create(
        CompanyId companyId,
        string title,
        string description,
        EcoPriority priority,
        UserId createdByUserId,
        DateTimeOffset? createdAt = null)
    {
        DomainGuard.AgainstDefault(companyId, nameof(companyId));
        DomainGuard.AgainstDefault(createdByUserId, nameof(createdByUserId));
        DomainGuard.AgainstInvalidEnum(priority, nameof(priority));

        var timestamp = DomainGuard.UtcTimestamp(createdAt);
        var eco = new EngineeringChangeOrder(
            EngineeringChangeOrderId.New(),
            companyId,
            DomainGuard.Required(title, nameof(title), 200),
            DomainGuard.Required(description, nameof(description), 4_000),
            priority,
            createdByUserId,
            timestamp);

        eco.AppendEvent(
            EcoEventType.Created,
            createdByUserId,
            null,
            EcoStatus.Draft,
            "ECO created.",
            timestamp);

        return eco;
    }

    public void UpdateDetails(
        string title,
        string description,
        EcoPriority priority,
        UserId actorUserId,
        DateTimeOffset? occurredAt = null)
    {
        EnsureEditable();
        DomainGuard.AgainstDefault(actorUserId, nameof(actorUserId));
        DomainGuard.AgainstInvalidEnum(priority, nameof(priority));

        var normalizedTitle = DomainGuard.Required(title, nameof(title), 200);
        var normalizedDescription = DomainGuard.Required(description, nameof(description), 4_000);

        if (Title == normalizedTitle && Description == normalizedDescription && Priority == priority)
        {
            return;
        }

        Title = normalizedTitle;
        Description = normalizedDescription;
        Priority = priority;
        UpdatedAt = DomainGuard.UtcTimestamp(occurredAt);

        AppendEvent(
            EcoEventType.DetailsUpdated,
            actorUserId,
            Status,
            Status,
            "ECO details updated.",
            UpdatedAt);
    }

    public void SubmitForReview(UserId actorUserId, DateTimeOffset? occurredAt = null)
    {
        TransitionTo(
            EcoStatus.UnderReview,
            actorUserId,
            EcoEventType.SubmittedForReview,
            "ECO submitted for review.",
            occurredAt);
    }

    public void Approve(UserId actorUserId, DateTimeOffset? occurredAt = null)
    {
        TransitionTo(
            EcoStatus.Approved,
            actorUserId,
            EcoEventType.Approved,
            "ECO approved.",
            occurredAt);
    }

    public void Reject(UserId actorUserId, string? reason = null, DateTimeOffset? occurredAt = null)
    {
        var description = string.IsNullOrWhiteSpace(reason)
            ? "ECO rejected."
            : $"ECO rejected: {DomainGuard.Required(reason, nameof(reason), 500)}";

        TransitionTo(
            EcoStatus.Rejected,
            actorUserId,
            EcoEventType.Rejected,
            description,
            occurredAt);
    }

    public void Implement(UserId actorUserId, DateTimeOffset? occurredAt = null)
    {
        TransitionTo(
            EcoStatus.Implemented,
            actorUserId,
            EcoEventType.Implemented,
            "ECO implemented.",
            occurredAt);
    }

    public bool CanTransitionTo(EcoStatus newStatus)
    {
        DomainGuard.AgainstInvalidEnum(newStatus, nameof(newStatus));
        return IsAllowedTransition(Status, newStatus);
    }

    private void TransitionTo(
        EcoStatus newStatus,
        UserId actorUserId,
        EcoEventType eventType,
        string description,
        DateTimeOffset? occurredAt)
    {
        DomainGuard.AgainstDefault(actorUserId, nameof(actorUserId));
        DomainGuard.AgainstInvalidEnum(newStatus, nameof(newStatus));
        DomainGuard.AgainstInvalidEnum(eventType, nameof(eventType));

        if (!IsAllowedTransition(Status, newStatus))
        {
            throw new DomainException($"ECO cannot transition from {Status} to {newStatus}.");
        }

        var oldStatus = Status;
        var timestamp = DomainGuard.UtcTimestamp(occurredAt);

        Status = newStatus;
        UpdatedAt = timestamp;

        AppendEvent(eventType, actorUserId, oldStatus, newStatus, description, timestamp);
    }

    private void EnsureEditable()
    {
        if (Status != EcoStatus.Draft)
        {
            throw new DomainException("Only draft ECOs can be edited.");
        }
    }

    private void AppendEvent(
        EcoEventType eventType,
        UserId actorUserId,
        EcoStatus? oldStatus,
        EcoStatus? newStatus,
        string description,
        DateTimeOffset occurredAt)
    {
        _events.Add(EcoEvent.Create(
            CompanyId,
            Id,
            actorUserId,
            eventType,
            description,
            oldStatus,
            newStatus,
            occurredAt));
    }

    private static bool IsAllowedTransition(EcoStatus currentStatus, EcoStatus newStatus)
    {
        return currentStatus switch
        {
            EcoStatus.Draft => newStatus == EcoStatus.UnderReview,
            EcoStatus.UnderReview => newStatus is EcoStatus.Approved or EcoStatus.Rejected,
            EcoStatus.Approved => newStatus == EcoStatus.Implemented,
            EcoStatus.Rejected or EcoStatus.Implemented => false,
            _ => false
        };
    }
}
