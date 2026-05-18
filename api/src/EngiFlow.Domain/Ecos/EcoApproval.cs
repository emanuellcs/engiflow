using EngiFlow.Domain.Abstractions;
using EngiFlow.Domain.Exceptions;
using EngiFlow.Domain.Guards;
using EngiFlow.Domain.ValueObjects;

namespace EngiFlow.Domain.Ecos;

/// <summary>
/// Approver decision for a specific ECO review round.
/// </summary>
public sealed class EcoApproval : ITenantScoped
{
    private EcoApproval()
    {
    }

    private EcoApproval(
        EcoApprovalId id,
        CompanyId companyId,
        EngineeringChangeOrderId engineeringChangeOrderId,
        UserId approverUserId,
        EcoApprovalDecision decision,
        int reviewRound,
        DateTimeOffset createdAt)
    {
        Id = id;
        CompanyId = companyId;
        EngineeringChangeOrderId = engineeringChangeOrderId;
        ApproverUserId = approverUserId;
        Decision = decision;
        ReviewRound = reviewRound;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    /// <summary>
    /// Gets the approval identifier.
    /// </summary>
    public EcoApprovalId Id { get; private set; }

    /// <inheritdoc />
    public CompanyId CompanyId { get; private set; }

    /// <summary>
    /// Gets the ECO identifier this decision belongs to.
    /// </summary>
    public EngineeringChangeOrderId EngineeringChangeOrderId { get; private set; }

    /// <summary>
    /// Gets the approver user identifier.
    /// </summary>
    public UserId ApproverUserId { get; private set; }

    /// <summary>
    /// Gets the current decision submitted by the approver.
    /// </summary>
    public EcoApprovalDecision Decision { get; private set; }

    /// <summary>
    /// Gets the ECO review round this decision applies to.
    /// </summary>
    public int ReviewRound { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when the decision record was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when the decision was last updated.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    internal static EcoApproval Create(
        CompanyId companyId,
        EngineeringChangeOrderId engineeringChangeOrderId,
        UserId approverUserId,
        EcoApprovalDecision decision,
        int reviewRound,
        DateTimeOffset? createdAt = null)
    {
        DomainGuard.AgainstDefault(companyId, nameof(companyId));
        DomainGuard.AgainstDefault(engineeringChangeOrderId, nameof(engineeringChangeOrderId));
        DomainGuard.AgainstDefault(approverUserId, nameof(approverUserId));
        DomainGuard.AgainstInvalidEnum(decision, nameof(decision));

        if (reviewRound <= 0)
        {
            throw new DomainException("Review round must be greater than zero.");
        }

        return new EcoApproval(
            EcoApprovalId.New(),
            companyId,
            engineeringChangeOrderId,
            approverUserId,
            decision,
            reviewRound,
            DomainGuard.UtcTimestamp(createdAt));
    }

    internal void UpdateDecision(EcoApprovalDecision decision, DateTimeOffset? updatedAt = null)
    {
        DomainGuard.AgainstInvalidEnum(decision, nameof(decision));
        Decision = decision;
        UpdatedAt = DomainGuard.UtcTimestamp(updatedAt);
    }
}
