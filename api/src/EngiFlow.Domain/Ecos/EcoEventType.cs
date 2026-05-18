namespace EngiFlow.Domain.Ecos;

/// <summary>
/// Classifies immutable audit entries recorded against an ECO.
/// </summary>
public enum EcoEventType
{
    /// <summary>
    /// The ECO aggregate was created in draft status.
    /// </summary>
    Created = 0,

    /// <summary>
    /// Draft ECO metadata was changed.
    /// </summary>
    DetailsUpdated = 1,

    /// <summary>
    /// The ECO moved from draft into formal review.
    /// </summary>
    SubmittedForReview = 2,

    /// <summary>
    /// The ECO received an approval or request-changes decision.
    /// </summary>
    ReviewDecisionSubmitted = 3,

    /// <summary>
    /// The ECO reached quorum and was approved.
    /// </summary>
    Approved = 4,

    /// <summary>
    /// An approver requested changes and returned the ECO to draft.
    /// </summary>
    ChangesRequested = 5,

    /// <summary>
    /// A draft affected item was added.
    /// </summary>
    AffectedItemAdded = 6,

    /// <summary>
    /// A draft affected item was removed.
    /// </summary>
    AffectedItemRemoved = 7,

    /// <summary>
    /// A timeline comment was added.
    /// </summary>
    CommentAdded = 8,

    /// <summary>
    /// An attachment metadata record was added.
    /// </summary>
    AttachmentAdded = 9,

    /// <summary>
    /// The ECO was canceled.
    /// </summary>
    Canceled = 10,

    /// <summary>
    /// Legacy event retained so existing persisted rows can still be materialized.
    /// </summary>
    Rejected = 11,

    /// <summary>
    /// Legacy event retained so existing persisted rows can still be materialized.
    /// </summary>
    Implemented = 12
}
