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
    /// The ECO has enough approval votes to be accepted.
    /// </summary>
    Approved = 2,

    /// <summary>
    /// The ECO has been canceled and is terminal.
    /// </summary>
    Canceled = 3,

    /// <summary>
    /// Legacy status retained so existing persisted rows can still be materialized.
    /// </summary>
    Rejected = 4,

    /// <summary>
    /// Legacy status retained so existing persisted rows can still be materialized.
    /// </summary>
    Implemented = 5
}
