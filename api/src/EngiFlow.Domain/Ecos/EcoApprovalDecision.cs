namespace EngiFlow.Domain.Ecos;

/// <summary>
/// Represents an approver's current decision for an ECO review round.
/// </summary>
public enum EcoApprovalDecision
{
    /// <summary>
    /// The approver accepts the current ECO proposal.
    /// </summary>
    Approve = 0,

    /// <summary>
    /// The approver requires the requester to revise the ECO.
    /// </summary>
    RequestChanges = 1
}
