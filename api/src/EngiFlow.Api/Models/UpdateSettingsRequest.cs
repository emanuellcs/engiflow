namespace EngiFlow.Api.Models;

/// <summary>
/// HTTP request body used to update tenant workflow governance settings.
/// </summary>
/// <param name="MinApprovalsRequired">Minimum approvals required for an ECO approval quorum.</param>
public sealed record UpdateSettingsRequest(int MinApprovalsRequired);
