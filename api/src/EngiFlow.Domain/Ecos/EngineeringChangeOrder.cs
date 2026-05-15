using EngiFlow.Domain.Abstractions;
using EngiFlow.Domain.Exceptions;
using EngiFlow.Domain.Guards;
using EngiFlow.Domain.ValueObjects;

namespace EngiFlow.Domain.Ecos;

/// <summary>
/// Aggregate root for the formal ECO approval workflow.
/// </summary>
/// <remarks>
/// The aggregate owns the ECO state machine and audit ledger because engineering
/// changes require a controlled approval path. Keeping transitions inside the domain
/// prevents callers from skipping review, approving rejected work, or implementing a
/// change without an auditable approval.
/// </remarks>
public sealed class EngineeringChangeOrder : ITenantScoped
{
    private readonly List<EcoEvent> _events = [];
    private readonly List<EcoEvent> _pendingEvents = [];

    /// <summary>
    /// Initializes a new empty instance of the <see cref="EngineeringChangeOrder"/> class for EF Core materialization.
    /// </summary>
    private EngineeringChangeOrder()
    {
    }

    /// <summary>
    /// Initializes a new draft ECO aggregate.
    /// </summary>
    /// <param name="id">The ECO identifier.</param>
    /// <param name="companyId">The tenant identifier that owns the ECO.</param>
    /// <param name="title">The ECO title.</param>
    /// <param name="description">The detailed change description.</param>
    /// <param name="priority">The operational priority.</param>
    /// <param name="createdByUserId">The user who created the ECO.</param>
    /// <param name="createdAt">The UTC creation timestamp.</param>
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

    /// <summary>
    /// Gets the unique ECO aggregate identifier.
    /// </summary>
    public EngineeringChangeOrderId Id { get; private set; }

    /// <summary>
    /// Gets the tenant identifier that owns the ECO.
    /// </summary>
    public CompanyId CompanyId { get; private set; }

    /// <summary>
    /// Gets the short business title of the requested engineering change.
    /// </summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the detailed description of the requested engineering change.
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the operational priority assigned to the ECO.
    /// </summary>
    public EcoPriority Priority { get; private set; }

    /// <summary>
    /// Gets the current lifecycle status of the ECO.
    /// </summary>
    public EcoStatus Status { get; private set; }

    /// <summary>
    /// Gets the user who originally created the ECO.
    /// </summary>
    public UserId CreatedByUserId { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when the ECO was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when the ECO was last changed.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// Gets the immutable audit timeline produced by this aggregate.
    /// </summary>
    /// <remarks>
    /// The backing collection is append-only inside the aggregate so callers cannot
    /// remove or rewrite the approval history.
    /// </remarks>
    public IReadOnlyCollection<EcoEvent> Events => _events.AsReadOnly();

    /// <summary>
    /// Gets the audit events produced by unsaved domain operations.
    /// </summary>
    /// <remarks>
    /// The aggregate keeps a separate pending-event buffer so infrastructure can persist
    /// newly produced audit records exactly once without confusing them with previously
    /// persisted timeline entries loaded from the database.
    /// </remarks>
    public IReadOnlyCollection<EcoEvent> PendingEvents => _pendingEvents.AsReadOnly();

    /// <summary>
    /// Clears the pending audit-event buffer after persistence has completed successfully.
    /// </summary>
    /// <remarks>
    /// Infrastructure calls this only after the surrounding unit of work is saved. Keeping
    /// this operation explicit prevents failed database writes from accidentally dropping
    /// audit events that still need to be retried.
    /// </remarks>
    public void ClearPendingEvents()
    {
        _pendingEvents.Clear();
    }

    /// <summary>
    /// Creates a draft ECO and records the initial audit event.
    /// </summary>
    /// <param name="companyId">The tenant identifier that owns the ECO.</param>
    /// <param name="title">The ECO title.</param>
    /// <param name="description">The detailed change description.</param>
    /// <param name="priority">The operational priority.</param>
    /// <param name="createdByUserId">The user who created the ECO.</param>
    /// <param name="createdAt">Optional timestamp used for deterministic tests or imports.</param>
    /// <returns>A new draft ECO with an audit event describing its creation.</returns>
    /// <exception cref="DomainException">Thrown when tenant, actor, title, description, or priority data is invalid.</exception>
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

    /// <summary>
    /// Updates draft ECO metadata and records an audit event when a change is made.
    /// </summary>
    /// <param name="title">The updated ECO title.</param>
    /// <param name="description">The updated change description.</param>
    /// <param name="priority">The updated operational priority.</param>
    /// <param name="actorUserId">The user performing the update.</param>
    /// <param name="occurredAt">Optional timestamp used for deterministic tests or imports.</param>
    /// <remarks>
    /// Only drafts are editable because review decisions must be made against a stable
    /// proposal. Once submitted, changes should be represented by a new ECO or a future
    /// explicit amendment workflow.
    /// </remarks>
    /// <exception cref="DomainException">Thrown when the ECO is not draft or supplied data is invalid.</exception>
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

    /// <summary>
    /// Moves the ECO from draft into formal review.
    /// </summary>
    /// <param name="actorUserId">The user submitting the ECO for review.</param>
    /// <param name="occurredAt">Optional timestamp used for deterministic tests or imports.</param>
    /// <remarks>
    /// The review step is mandatory so engineering changes cannot be approved without
    /// first being visible to reviewers and approvers.
    /// </remarks>
    public void SubmitForReview(UserId actorUserId, DateTimeOffset? occurredAt = null)
    {
        TransitionTo(
            EcoStatus.UnderReview,
            actorUserId,
            EcoEventType.SubmittedForReview,
            "ECO submitted for review.",
            occurredAt);
    }

    /// <summary>
    /// Approves an ECO that is currently under review.
    /// </summary>
    /// <param name="actorUserId">The user approving the ECO.</param>
    /// <param name="occurredAt">Optional timestamp used for deterministic tests or imports.</param>
    /// <remarks>
    /// Approval is separated from implementation so the organization has a clear record
    /// of the decision before physical, CAD, material, or process changes are applied.
    /// </remarks>
    public void Approve(UserId actorUserId, DateTimeOffset? occurredAt = null)
    {
        TransitionTo(
            EcoStatus.Approved,
            actorUserId,
            EcoEventType.Approved,
            "ECO approved.",
            occurredAt);
    }

    /// <summary>
    /// Rejects an ECO that is currently under review.
    /// </summary>
    /// <param name="actorUserId">The user rejecting the ECO.</param>
    /// <param name="reason">Optional business reason for the rejection.</param>
    /// <param name="occurredAt">Optional timestamp used for deterministic tests or imports.</param>
    /// <remarks>
    /// Rejection is terminal in this foundation so rejected decisions cannot later be
    /// silently converted into approved or implemented changes.
    /// </remarks>
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

    /// <summary>
    /// Marks an approved ECO as implemented.
    /// </summary>
    /// <param name="actorUserId">The user confirming implementation.</param>
    /// <param name="occurredAt">Optional timestamp used for deterministic tests or imports.</param>
    /// <remarks>
    /// Implementation is terminal because it represents the point at which the approved
    /// engineering change has been applied.
    /// </remarks>
    public void Implement(UserId actorUserId, DateTimeOffset? occurredAt = null)
    {
        TransitionTo(
            EcoStatus.Implemented,
            actorUserId,
            EcoEventType.Implemented,
            "ECO implemented.",
            occurredAt);
    }

    /// <summary>
    /// Determines whether the ECO can move from its current status to the requested status.
    /// </summary>
    /// <param name="newStatus">The requested next lifecycle status.</param>
    /// <returns><see langword="true"/> when the transition is allowed; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="DomainException">Thrown when <paramref name="newStatus"/> is not a declared status.</exception>
    public bool CanTransitionTo(EcoStatus newStatus)
    {
        DomainGuard.AgainstInvalidEnum(newStatus, nameof(newStatus));
        return IsAllowedTransition(Status, newStatus);
    }

    /// <summary>
    /// Applies a validated lifecycle transition and appends its audit event.
    /// </summary>
    /// <param name="newStatus">The next lifecycle status.</param>
    /// <param name="actorUserId">The user performing the transition.</param>
    /// <param name="eventType">The audit event type associated with the transition.</param>
    /// <param name="description">The audit event description.</param>
    /// <param name="occurredAt">Optional timestamp used for deterministic tests or imports.</param>
    /// <exception cref="DomainException">Thrown when the transition is not allowed or supplied data is invalid.</exception>
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

    /// <summary>
    /// Ensures the ECO can still be edited.
    /// </summary>
    /// <exception cref="DomainException">Thrown when the ECO is no longer in draft status.</exception>
    private void EnsureEditable()
    {
        if (Status != EcoStatus.Draft)
        {
            throw new DomainException("Only draft ECOs can be edited.");
        }
    }

    /// <summary>
    /// Appends an immutable event to the ECO audit timeline.
    /// </summary>
    /// <param name="eventType">The audit event type.</param>
    /// <param name="actorUserId">The user who performed the audited action.</param>
    /// <param name="oldStatus">The ECO status before the action, when applicable.</param>
    /// <param name="newStatus">The ECO status after the action, when applicable.</param>
    /// <param name="description">The audit event description.</param>
    /// <param name="occurredAt">The UTC timestamp when the action occurred.</param>
    private void AppendEvent(
        EcoEventType eventType,
        UserId actorUserId,
        EcoStatus? oldStatus,
        EcoStatus? newStatus,
        string description,
        DateTimeOffset occurredAt)
    {
        var ecoEvent = EcoEvent.Create(
            CompanyId,
            Id,
            actorUserId,
            eventType,
            description,
            oldStatus,
            newStatus,
            occurredAt);

        _events.Add(ecoEvent);
        _pendingEvents.Add(ecoEvent);
    }

    /// <summary>
    /// Encodes the ECO lifecycle transition table.
    /// </summary>
    /// <param name="currentStatus">The current ECO status.</param>
    /// <param name="newStatus">The requested next ECO status.</param>
    /// <returns><see langword="true"/> when the transition is permitted by the approval workflow.</returns>
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
