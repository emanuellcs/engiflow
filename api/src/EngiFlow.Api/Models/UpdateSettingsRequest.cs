namespace EngiFlow.Api.Models;

/// <summary>
/// HTTP request body used to update tenant workflow governance settings.
/// </summary>
/// <param name="MinApprovalsRequired">Minimum approvals required for an ECO approval quorum.</param>
/// <param name="MaxReviewDaysBeforeSlabreach">Maximum review time before SLA breach (Days).</param>
/// <param name="AllowSelfApproval">Whether ECO authors can approve their own submissions.</param>
public sealed record UpdateSettingsRequest(
    int MinApprovalsRequired,
    int MaxReviewDaysBeforeSlabreach,
    bool AllowSelfApproval);
