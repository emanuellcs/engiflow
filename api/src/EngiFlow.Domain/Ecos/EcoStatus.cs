namespace EngiFlow.Domain.Ecos;

/// <summary>
/// Represents the lifecycle state of an engineering change order.
/// </summary>
/// <remarks>
/// ECO status is modeled as a strict state machine because engineering changes require
/// traceable review and approval before they can be implemented.
/// </remarks>
public enum EcoStatus
{
    /// <summary>
    /// The ECO can still be edited by the requester before formal review starts.
    /// </summary>
    Draft = 0,

    /// <summary>
    /// The ECO is locked for review and awaits an approval or rejection decision.
    /// </summary>
    UnderReview = 1,

    /// <summary>
    /// The ECO has been accepted and can move to implementation.
    /// </summary>
    Approved = 2,

    /// <summary>
    /// The ECO has been declined and is terminal.
    /// </summary>
    Rejected = 3,

    /// <summary>
    /// The approved change has been applied and the ECO is terminal.
    /// </summary>
    Implemented = 4
}
