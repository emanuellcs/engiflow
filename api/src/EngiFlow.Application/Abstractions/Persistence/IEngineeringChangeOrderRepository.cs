using EngiFlow.Domain.Ecos;
using EngiFlow.Domain.ValueObjects;

namespace EngiFlow.Application.Abstractions.Persistence;

/// <summary>
/// Provides tenant-scoped persistence operations for engineering change order aggregates.
/// </summary>
/// <remarks>
/// Application handlers depend on this contract so use cases can orchestrate the domain
/// model without depending on Entity Framework Core or a specific database provider.
/// </remarks>
public interface IEngineeringChangeOrderRepository
{
    /// <summary>
    /// Adds a newly created ECO aggregate to the current unit of work.
    /// </summary>
    /// <param name="eco">The ECO aggregate to persist.</param>
    /// <param name="cancellationToken">A token that can cancel the persistence operation.</param>
    /// <returns>A task that completes when the aggregate has been staged.</returns>
    Task AddAsync(EngineeringChangeOrder eco, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds an ECO aggregate by identifier within the current tenant.
    /// </summary>
    /// <param name="id">The ECO identifier.</param>
    /// <param name="cancellationToken">A token that can cancel the persistence operation.</param>
    /// <returns>The ECO aggregate when found; otherwise, <see langword="null"/>.</returns>
    Task<EngineeringChangeOrder?> GetByIdAsync(
        EngineeringChangeOrderId id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds an ECO aggregate with its audit history by identifier within the current tenant.
    /// </summary>
    /// <param name="id">The ECO identifier.</param>
    /// <param name="cancellationToken">A token that can cancel the persistence operation.</param>
    /// <returns>The ECO aggregate and events when found; otherwise, <see langword="null"/>.</returns>
    Task<EngineeringChangeOrder?> GetByIdWithEventsAsync(
        EngineeringChangeOrderId id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists a single tenant-scoped page of ECO aggregates ordered for recent-work displays.
    /// </summary>
    /// <param name="pageNumber">The one-based page number.</param>
    /// <param name="pageSize">The number of rows to include in the page.</param>
    /// <param name="cancellationToken">A token that can cancel the persistence operation.</param>
    /// <returns>The requested page of ECO aggregates.</returns>
    Task<IReadOnlyList<EngineeringChangeOrder>> ListAsync(
        int pageNumber,
        int pageSize,
        EcoListFilter? filter = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts all ECO aggregates visible to the current tenant.
    /// </summary>
    /// <param name="filter">Optional list filtering criteria.</param>
    /// <param name="cancellationToken">A token that can cancel the persistence operation.</param>
    /// <returns>The total ECO count for the current tenant.</returns>
    Task<int> CountAsync(EcoListFilter? filter = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists the most recent audit events across all ECOs in the current tenant.
    /// </summary>
    /// <param name="limit">The maximum number of events to return.</param>
    /// <param name="cancellationToken">A token that can cancel the persistence operation.</param>
    /// <returns>The requested list of recent audit events with actor and ECO context.</returns>
    Task<IReadOnlyList<EcoActivityDto>> ListRecentActivityAsync(int limit, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a rich ECO activity entry for dashboard feeds.
/// </summary>
public sealed record EcoActivityDto(
    EcoEvent Event,
    string ActorName,
    string EcoTitle);

/// <summary>
/// Defines optional tenant-scoped ECO list filtering criteria.
/// </summary>
public sealed record EcoListFilter(
    string? Search,
    EcoStatus? Status,
    EcoPriority? Priority,
    DateTimeOffset? CreatedFrom,
    DateTimeOffset? CreatedTo,
    UserId? CreatedByUserId,
    UserId? AwaitingReviewByUserId);
