using EngiFlow.Application.Abstractions.Cqrs;
using EngiFlow.Application.Abstractions.Persistence;
using EngiFlow.Application.Abstractions.Tenancy;
using EngiFlow.Application.Ecos.Dtos;
using EngiFlow.Domain.Ecos;
using EngiFlow.Domain.ValueObjects;
using FluentValidation;

namespace EngiFlow.Application.Ecos.Queries;

/// <summary>
/// Query that retrieves a tenant-scoped page of ECO summaries.
/// </summary>
/// <param name="PageNumber">The one-based page number to retrieve.</param>
/// <param name="PageSize">The number of ECOs to include in the page.</param>
/// <param name="Search">Optional case-insensitive text search across title and description.</param>
/// <param name="Status">Optional lifecycle status filter.</param>
/// <param name="Priority">Optional priority filter.</param>
/// <param name="CreatedFrom">Optional inclusive created-at lower bound.</param>
/// <param name="CreatedTo">Optional inclusive created-at upper bound.</param>
/// <param name="CreatedByMe">Whether to show only ECOs created by the current actor.</param>
/// <param name="AwaitingMyReview">Whether to show under-review ECOs missing the current actor's active-round review decision.</param>
public sealed record ListEcosQuery(
    int PageNumber = 1,
    int PageSize = 20,
    string? Search = null,
    EcoStatus? Status = null,
    EcoPriority? Priority = null,
    DateTimeOffset? CreatedFrom = null,
    DateTimeOffset? CreatedTo = null,
    bool CreatedByMe = false,
    bool AwaitingMyReview = false)
    : IQuery<PagedResult<EcoSummaryDto>>;

/// <summary>
/// Validates <see cref="ListEcosQuery"/> requests before paginated retrieval.
/// </summary>
public sealed class ListEcosQueryValidator : AbstractValidator<ListEcosQuery>
{
    /// <summary>
    /// Initializes validation rules for listing ECOs.
    /// </summary>
    public ListEcosQueryValidator()
    {
        RuleFor(query => query.PageNumber)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Page number must be greater than or equal to 1.");

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("Page size must be between 1 and 100.");

        RuleFor(query => query.Search)
            .MaximumLength(200)
            .WithMessage("Search cannot exceed 200 characters.");

        RuleFor(query => query)
            .Must(query =>
                query.CreatedFrom is null ||
                query.CreatedTo is null ||
                query.CreatedFrom <= query.CreatedTo)
            .WithMessage("Created from date must be earlier than or equal to created to date.");
    }
}

/// <summary>
/// Handles paginated retrieval of tenant-scoped ECO summaries.
/// </summary>
public sealed class ListEcosQueryHandler : IQueryHandler<ListEcosQuery, PagedResult<EcoSummaryDto>>
{
    private readonly IEngineeringChangeOrderRepository _ecos;
    private readonly ITenantProvider _tenantProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListEcosQueryHandler"/> class.
    /// </summary>
    /// <param name="ecos">The ECO repository.</param>
    /// <param name="tenantProvider">The current tenant and actor context.</param>
    public ListEcosQueryHandler(
        IEngineeringChangeOrderRepository ecos,
        ITenantProvider tenantProvider)
    {
        _ecos = ecos;
        _tenantProvider = tenantProvider;
    }

    /// <inheritdoc />
    public async Task<PagedResult<EcoSummaryDto>> HandleAsync(
        ListEcosQuery query,
        CancellationToken cancellationToken = default)
    {
        var filter = new EcoListFilter(
            query.Search,
            query.Status,
            query.Priority,
            query.CreatedFrom,
            query.CreatedTo,
            query.CreatedByMe ? _tenantProvider.CurrentUserId : null,
            query.AwaitingMyReview ? _tenantProvider.CurrentUserId : null);
        var totalCount = await _ecos.CountAsync(filter, cancellationToken).ConfigureAwait(false);
        var ecos = await _ecos.ListAsync(query.PageNumber, query.PageSize, filter, cancellationToken)
            .ConfigureAwait(false);

        return new PagedResult<EcoSummaryDto>(
            ecos.Select(eco => eco.ToSummaryDto()).ToArray(),
            query.PageNumber,
            query.PageSize,
            totalCount);
    }
}
