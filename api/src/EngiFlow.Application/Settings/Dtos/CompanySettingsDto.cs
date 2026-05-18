namespace EngiFlow.Application.Settings.Dtos;

/// <summary>
/// Describes tenant workflow governance settings.
/// </summary>
/// <param name="MinApprovalsRequired">Minimum approvals required for an ECO review quorum.</param>
/// <param name="UpdatedAt">The UTC timestamp when settings were last updated.</param>
public sealed record CompanySettingsDto(
    int MinApprovalsRequired,
    DateTimeOffset UpdatedAt);
