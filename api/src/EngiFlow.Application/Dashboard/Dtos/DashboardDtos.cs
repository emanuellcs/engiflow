namespace EngiFlow.Application.Dashboard.Dtos;

/// <summary>
/// Represents a single numerical metric displayed on the dashboard.
/// </summary>
/// <param name="Label">The descriptive label for the metric (e.g., "Waiting for your review").</param>
/// <param name="Value">The numerical count.</param>
/// <param name="Footer">A small caption footer (e.g., "Just now").</param>
public sealed record DashboardMetricDto(string Label, int Value, string Footer);

/// <summary>
/// Represents an actionable item requiring user attention or a recent workspace activity.
/// </summary>
/// <param name="Id">The unique identifier of the entity (e.g., ECO ID).</param>
/// <param name="Title">A descriptive title for the action item.</param>
/// <param name="Subtitle">A muted subtitle providing context (e.g., priority or status).</param>
/// <param name="Type">The category of the item for deep-linking (e.g., "ECO").</param>
/// <param name="DeepLink">The specific path for immediate navigation (e.g., "/ecos/ECO-2026-004").</param>
/// <param name="UserName">The name of the user who performed the action, if applicable.</param>
/// <param name="EcoTitle">The abbreviated title of the related ECO, if applicable.</param>
/// <param name="OccurredAt">The UTC timestamp when the activity occurred, if applicable.</param>
public sealed record DashboardAttentionItemDto(
    Guid Id,
    string Title,
    string Subtitle,
    string Type,
    string DeepLink,
    string? UserName = null,
    string? EcoTitle = null,
    DateTimeOffset? OccurredAt = null);

/// <summary>
/// Aggregates all data required to render the role-based dashboard view.
/// </summary>
/// <param name="Metrics">The list of numerical metrics tailored to the user's role.</param>
/// <param name="AttentionItems">The list of actionable items requiring user attention.</param>
public sealed record DashboardDataDto(
    IReadOnlyList<DashboardMetricDto> Metrics,
    IReadOnlyList<DashboardAttentionItemDto> AttentionItems);
