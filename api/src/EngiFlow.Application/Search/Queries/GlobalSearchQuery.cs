using EngiFlow.Application.Abstractions.Cqrs;
using EngiFlow.Application.Abstractions.Persistence;
using EngiFlow.Application.Search.Dtos;

namespace EngiFlow.Application.Search.Queries;

/// <summary>
/// Query that performs an optimized, tenant-scoped search across ECOs and Team Members.
/// </summary>
/// <param name="QueryTerm">The search string supplied by the user.</param>
public sealed record GlobalSearchQuery(string QueryTerm) : IQuery<GlobalSearchResultDto>;

/// <summary>
/// Handles aggregated search execution with strict tenant isolation and result capping.
/// </summary>
public sealed class GlobalSearchQueryHandler : IQueryHandler<GlobalSearchQuery, GlobalSearchResultDto>
{
    private readonly IEngineeringChangeOrderRepository _ecos;
    private readonly IUserRepository _users;

    /// <summary>
    /// Initializes a new instance of the <see cref="GlobalSearchQueryHandler"/> class.
    /// </summary>
    /// <param name="ecos">The ECO repository.</param>
    /// <param name="users">The User repository.</param>
    public GlobalSearchQueryHandler(
        IEngineeringChangeOrderRepository ecos,
        IUserRepository users)
    {
        _ecos = ecos;
        _users = users;
    }

    /// <inheritdoc />
    public async Task<GlobalSearchResultDto> HandleAsync(
        GlobalSearchQuery query,
        CancellationToken cancellationToken = default)
    {
        var term = query.QueryTerm.Trim();
        if (string.IsNullOrWhiteSpace(term))
        {
            return new GlobalSearchResultDto([], []);
        }

        // Fetch top 5 ECOs matching Code (ID), Title, or Description.
        var ecoFilter = new EcoListFilter(
            Search: term,
            Status: null,
            Priority: null,
            CreatedFrom: null,
            CreatedTo: null,
            CreatedByUserId: null,
            AwaitingReviewByUserId: null);

        var ecos = await _ecos.ListAsync(1, 5, ecoFilter, cancellationToken)
            .ConfigureAwait(false);

        // Fetch top 5 Users matching Display Name or Email.
        var users = await _users.SearchAsync(term, 5, cancellationToken)
            .ConfigureAwait(false);

        return new GlobalSearchResultDto(
            ecos.Select(e => new EcoSearchResultDto(e.Id.Value, e.Title, e.Description, e.Status)).ToArray(),
            users.Select(u => new UserSearchResultDto(u.Id.Value, u.DisplayName, u.Email, u.Role)).ToArray());
    }
}
