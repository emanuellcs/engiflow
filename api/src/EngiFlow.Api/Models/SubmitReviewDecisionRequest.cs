using EngiFlow.Domain.Ecos;

namespace EngiFlow.Api.Models;

/// <summary>
/// HTTP request body used to submit an ECO review decision.
/// </summary>
/// <param name="Decision">The approver decision.</param>
/// <param name="Comment">An optional review comment.</param>
public sealed record SubmitReviewDecisionRequest(
    EcoApprovalDecision Decision,
    string? Comment);
