using EngiFlow.Domain.Abstractions;
using EngiFlow.Domain.Exceptions;
using EngiFlow.Domain.Guards;
using EngiFlow.Domain.ValueObjects;

namespace EngiFlow.Domain.Ecos;

/// <summary>
/// Aggregate root for the pull-request-style ECO workflow.
/// </summary>
/// <remarks>
/// The aggregate owns the editable draft, review round, approval quorum, and append-only
/// timeline so callers cannot bypass review or record inconsistent audit history.
/// </remarks>
public sealed class EngineeringChangeOrder : ITenantScoped
{
    private readonly List<EcoAffectedItem> _affectedItems = [];
    private readonly List<EcoApproval> _approvals = [];
    private readonly List<EcoAttachment> _attachments = [];
    private readonly List<EcoComment> _comments = [];
    private readonly List<EcoEvent> _events = [];
    private readonly List<EcoEvent> _pendingEvents = [];

    /// <summary>
    /// Initializes a new empty instance of the <see cref="EngineeringChangeOrder"/> class for EF Core materialization.
    /// </summary>
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
        ReviewRound = 0;
    }

    /// <summary>
    /// Gets the unique ECO aggregate identifier.
    /// </summary>
    public EngineeringChangeOrderId Id { get; private set; }

    /// <inheritdoc />
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
    /// Gets the current review round. Draft ECOs that have never been submitted are round zero.
    /// </summary>
    public int ReviewRound { get; private set; }

    /// <summary>
    /// Gets PostgreSQL's xmin-backed optimistic concurrency token.
    /// </summary>
    public uint RowVersion { get; private set; }

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
    public IReadOnlyCollection<EcoEvent> Events => _events.AsReadOnly();

    /// <summary>
    /// Gets user-authored timeline comments.
    /// </summary>
    public IReadOnlyCollection<EcoComment> Comments => _comments.AsReadOnly();

    /// <summary>
    /// Gets the engineering diff rows attached to the ECO.
    /// </summary>
    public IReadOnlyCollection<EcoAffectedItem> AffectedItems => _affectedItems.AsReadOnly();

    /// <summary>
    /// Gets review decisions submitted against all review rounds.
    /// </summary>
    public IReadOnlyCollection<EcoApproval> Approvals => _approvals.AsReadOnly();

    /// <summary>
    /// Gets S3-compatible attachment metadata records.
    /// </summary>
    public IReadOnlyCollection<EcoAttachment> Attachments => _attachments.AsReadOnly();

    /// <summary>
    /// Gets audit events produced by unsaved domain operations.
    /// </summary>
    public IReadOnlyCollection<EcoEvent> PendingEvents => _pendingEvents.AsReadOnly();

    /// <summary>
    /// Clears pending audit events after persistence has completed successfully.
    /// </summary>
    public void ClearPendingEvents()
    {
        _pendingEvents.Clear();
    }

    /// <summary>
    /// Creates a draft ECO and records the initial audit event.
    /// </summary>
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
    /// Updates draft ECO metadata.
    /// </summary>
    public void UpdateDetails(
        string title,
        string description,
        EcoPriority priority,
        UserId actorUserId,
        DateTimeOffset? occurredAt = null)
    {
        EnsureDraftEditable();
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
    /// Adds an engineering diff row while the ECO is draft.
    /// </summary>
    public EcoAffectedItem AddAffectedItem(
        string partNumber,
        string description,
        string currentRevision,
        string newRevision,
        EcoAffectedItemAction action,
        UserId actorUserId,
        DateTimeOffset? occurredAt = null)
    {
        EnsureDraftEditable();
        DomainGuard.AgainstDefault(actorUserId, nameof(actorUserId));
        var timestamp = DomainGuard.UtcTimestamp(occurredAt);
        var affectedItem = EcoAffectedItem.Create(
            CompanyId,
            Id,
            partNumber,
            description,
            currentRevision,
            newRevision,
            action,
            actorUserId,
            timestamp);

        _affectedItems.Add(affectedItem);
        UpdatedAt = timestamp;
        AppendEvent(
            EcoEventType.AffectedItemAdded,
            actorUserId,
            Status,
            Status,
            $"Affected item '{affectedItem.PartNumber}' added.",
            timestamp);

        return affectedItem;
    }

    /// <summary>
    /// Removes an engineering diff row while the ECO is draft.
    /// </summary>
    public void RemoveAffectedItem(
        EcoAffectedItemId affectedItemId,
        UserId actorUserId,
        DateTimeOffset? occurredAt = null)
    {
        EnsureDraftEditable();
        DomainGuard.AgainstDefault(affectedItemId, nameof(affectedItemId));
        DomainGuard.AgainstDefault(actorUserId, nameof(actorUserId));

        var affectedItem = _affectedItems.SingleOrDefault(item => item.Id == affectedItemId)
            ?? throw new DomainException("Affected item was not found on this ECO.");
        var timestamp = DomainGuard.UtcTimestamp(occurredAt);

        _affectedItems.Remove(affectedItem);
        UpdatedAt = timestamp;
        AppendEvent(
            EcoEventType.AffectedItemRemoved,
            actorUserId,
            Status,
            Status,
            $"Affected item '{affectedItem.PartNumber}' removed.",
            timestamp);
    }

    /// <summary>
    /// Adds a user-authored comment to the ECO timeline unless the ECO is canceled.
    /// </summary>
    public EcoComment AddComment(
        string body,
        UserId actorUserId,
        DateTimeOffset? occurredAt = null)
    {
        EnsureNotCanceled();
        DomainGuard.AgainstDefault(actorUserId, nameof(actorUserId));
        var timestamp = DomainGuard.UtcTimestamp(occurredAt);
        var comment = EcoComment.Create(CompanyId, Id, actorUserId, body, timestamp);

        _comments.Add(comment);
        UpdatedAt = timestamp;
        AppendEvent(
            EcoEventType.CommentAdded,
            actorUserId,
            Status,
            Status,
            "Comment added.",
            timestamp);

        return comment;
    }

    /// <summary>
    /// Adds S3-compatible attachment metadata while the ECO is draft.
    /// </summary>
    public EcoAttachment AddAttachment(
        string fileName,
        long fileSize,
        string objectKey,
        string mimeType,
        UserId actorUserId,
        DateTimeOffset? occurredAt = null)
    {
        EnsureDraftEditable();
        DomainGuard.AgainstDefault(actorUserId, nameof(actorUserId));
        var timestamp = DomainGuard.UtcTimestamp(occurredAt);
        var attachment = EcoAttachment.Create(
            CompanyId,
            Id,
            fileName,
            fileSize,
            objectKey,
            mimeType,
            actorUserId,
            timestamp);

        _attachments.Add(attachment);
        UpdatedAt = timestamp;
        AppendEvent(
            EcoEventType.AttachmentAdded,
            actorUserId,
            Status,
            Status,
            $"Attachment '{attachment.FileName}' added.",
            timestamp);

        return attachment;
    }

    /// <summary>
    /// Moves the ECO from draft into formal review and starts a new review round.
    /// </summary>
    public void SubmitForReview(UserId actorUserId, DateTimeOffset? occurredAt = null)
    {
        DomainGuard.AgainstDefault(actorUserId, nameof(actorUserId));

        if (Status != EcoStatus.Draft)
        {
            throw new DomainException($"ECO cannot transition from {Status} to {EcoStatus.UnderReview}.");
        }

        var oldStatus = Status;
        var timestamp = DomainGuard.UtcTimestamp(occurredAt);
        ReviewRound++;
        Status = EcoStatus.UnderReview;
        UpdatedAt = timestamp;

        AppendEvent(
            EcoEventType.SubmittedForReview,
            actorUserId,
            oldStatus,
            Status,
            $"ECO submitted for review round {ReviewRound}.",
            timestamp);
    }

    /// <summary>
    /// Records an approval decision and applies quorum or request-changes transitions.
    /// </summary>
    public void SubmitReviewDecision(
        UserId approverUserId,
        EcoApprovalDecision decision,
        int minApprovalsRequired,
        string? comment = null,
        DateTimeOffset? occurredAt = null)
    {
        DomainGuard.AgainstDefault(approverUserId, nameof(approverUserId));
        DomainGuard.AgainstInvalidEnum(decision, nameof(decision));

        if (Status != EcoStatus.UnderReview)
        {
            throw new DomainException("Review decisions can only be submitted while an ECO is under review.");
        }

        if (approverUserId == CreatedByUserId)
        {
            throw new DomainException("Compliance Rule: The author of the ECO cannot participate in its approval quorum");
        }

        if (minApprovalsRequired < 1)
        {
            throw new DomainException("Minimum approvals required must be at least one.");
        }

        var timestamp = DomainGuard.UtcTimestamp(occurredAt);
        UpsertApproval(approverUserId, decision, timestamp);
        UpdatedAt = timestamp;

        if (!string.IsNullOrWhiteSpace(comment))
        {
            _comments.Add(EcoComment.Create(CompanyId, Id, approverUserId, comment, timestamp));
        }

        AppendEvent(
            EcoEventType.ReviewDecisionSubmitted,
            approverUserId,
            Status,
            Status,
            decision == EcoApprovalDecision.Approve
                ? $"Approval submitted for review round {ReviewRound}."
                : $"Changes requested for review round {ReviewRound}.",
            timestamp);

        if (decision == EcoApprovalDecision.RequestChanges)
        {
            var oldStatus = Status;
            Status = EcoStatus.Draft;
            AppendEvent(
                EcoEventType.ChangesRequested,
                approverUserId,
                oldStatus,
                Status,
                "ECO returned to draft because changes were requested.",
                timestamp);
            return;
        }

        if (CurrentRoundApprovalCount() >= minApprovalsRequired)
        {
            var oldStatus = Status;
            Status = EcoStatus.Approved;
            AppendEvent(
                EcoEventType.Approved,
                approverUserId,
                oldStatus,
                Status,
                "ECO approved by quorum.",
                timestamp);
        }
    }

    /// <summary>
    /// Cancels a nonterminal ECO.
    /// </summary>
    public void Cancel(UserId actorUserId, DateTimeOffset? occurredAt = null)
    {
        DomainGuard.AgainstDefault(actorUserId, nameof(actorUserId));

        if (Status is EcoStatus.Canceled or EcoStatus.Approved or EcoStatus.Rejected or EcoStatus.Implemented)
        {
            throw new DomainException($"ECO cannot transition from {Status} to {EcoStatus.Canceled}.");
        }

        var oldStatus = Status;
        var timestamp = DomainGuard.UtcTimestamp(occurredAt);
        Status = EcoStatus.Canceled;
        UpdatedAt = timestamp;

        AppendEvent(
            EcoEventType.Canceled,
            actorUserId,
            oldStatus,
            Status,
            "ECO canceled.",
            timestamp);
    }

    /// <summary>
    /// Legacy approval transition retained for compatibility with older application tests and callers.
    /// </summary>
    public void Approve(UserId actorUserId, DateTimeOffset? occurredAt = null)
    {
        SubmitReviewDecision(actorUserId, EcoApprovalDecision.Approve, 1, null, occurredAt);
    }

    /// <summary>
    /// Legacy rejection transition now maps to request changes and returns the ECO to draft.
    /// </summary>
    public void Reject(UserId actorUserId, string? reason = null, DateTimeOffset? occurredAt = null)
    {
        SubmitReviewDecision(actorUserId, EcoApprovalDecision.RequestChanges, 1, reason, occurredAt);
    }

    /// <summary>
    /// Legacy implementation transition is no longer part of the active workflow.
    /// </summary>
    public void Implement(UserId actorUserId, DateTimeOffset? occurredAt = null)
    {
        throw new DomainException("Approved ECOs are terminal in the current workflow.");
    }

    /// <summary>
    /// Determines whether the ECO can move from its current status to the requested status.
    /// </summary>
    public bool CanTransitionTo(EcoStatus newStatus)
    {
        DomainGuard.AgainstInvalidEnum(newStatus, nameof(newStatus));
        return Status switch
        {
            EcoStatus.Draft => newStatus is EcoStatus.UnderReview or EcoStatus.Canceled,
            EcoStatus.UnderReview => newStatus is EcoStatus.Draft or EcoStatus.Approved or EcoStatus.Canceled,
            EcoStatus.Approved or EcoStatus.Canceled or EcoStatus.Rejected or EcoStatus.Implemented => false,
            _ => false
        };
    }

    private void UpsertApproval(UserId approverUserId, EcoApprovalDecision decision, DateTimeOffset timestamp)
    {
        var approval = _approvals.SingleOrDefault(candidate =>
            candidate.ReviewRound == ReviewRound && candidate.ApproverUserId == approverUserId);

        if (approval is null)
        {
            _approvals.Add(EcoApproval.Create(
                CompanyId,
                Id,
                approverUserId,
                decision,
                ReviewRound,
                timestamp));
            return;
        }

        approval.UpdateDecision(decision, timestamp);
    }

    private int CurrentRoundApprovalCount()
    {
        return _approvals.Count(approval =>
            approval.ReviewRound == ReviewRound && approval.Decision == EcoApprovalDecision.Approve);
    }

    private void EnsureDraftEditable()
    {
        if (Status != EcoStatus.Draft)
        {
            throw new DomainException("Only draft ECOs can be edited.");
        }
    }

    private void EnsureNotCanceled()
    {
        if (Status == EcoStatus.Canceled)
        {
            throw new DomainException("Canceled ECOs cannot be changed.");
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
}
