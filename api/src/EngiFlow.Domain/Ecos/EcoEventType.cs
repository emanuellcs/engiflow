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
    /// The ECO was approved by an authorized actor.
    /// </summary>
    Approved = 3,

    /// <summary>
    /// The ECO was rejected and is no longer eligible for implementation.
    /// </summary>
    Rejected = 4,

    /// <summary>
    /// The approved engineering change was implemented.
    /// </summary>
    Implemented = 5
}
