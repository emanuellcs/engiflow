using EngiFlow.Application.Abstractions.Cqrs;
using EngiFlow.Application.Abstractions.Persistence;
using EngiFlow.Application.Ecos.Dtos;
using FluentValidation;

namespace EngiFlow.Application.Ecos.Queries;

/// <summary>
/// Query that retrieves a tenant-scoped page of ECO summaries.
/// </summary>
/// <param name="PageNumber">The one-based page number to retrieve.</param>
/// <param name="PageSize">The number of ECOs to include in the page.</param>
public sealed record ListEcosQuery(int PageNumber = 1, int PageSize = 20)
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
    }
}

/// <summary>
/// Handles paginated retrieval of tenant-scoped ECO summaries.
/// </summary>
public sealed class ListEcosQueryHandler : IQueryHandler<ListEcosQuery, PagedResult<EcoSummaryDto>>
{
    private readonly IEngineeringChangeOrderRepository _ecos;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListEcosQueryHandler"/> class.
    /// </summary>
    /// <param name="ecos">The ECO repository.</param>
    public ListEcosQueryHandler(IEngineeringChangeOrderRepository ecos)
    {
        _ecos = ecos;
    }

    /// <inheritdoc />
    public async Task<PagedResult<EcoSummaryDto>> HandleAsync(
        ListEcosQuery query,
        CancellationToken cancellationToken = default)
    {
        var totalCount = await _ecos.CountAsync(cancellationToken).ConfigureAwait(false);
        var ecos = await _ecos.ListAsync(query.PageNumber, query.PageSize, cancellationToken)
            .ConfigureAwait(false);

        return new PagedResult<EcoSummaryDto>(
            ecos.Select(eco => eco.ToSummaryDto()).ToArray(),
            query.PageNumber,
            query.PageSize,
            totalCount);
    }
}
