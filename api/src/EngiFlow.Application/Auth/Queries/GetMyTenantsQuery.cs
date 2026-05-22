using EngiFlow.Application.Abstractions.Cqrs;
using EngiFlow.Application.Abstractions.Persistence;
using EngiFlow.Application.Abstractions.Tenancy;
using EngiFlow.Application.Auth.Dtos;
using EngiFlow.Application.Exceptions;

namespace EngiFlow.Application.Auth.Queries;

/// <summary>
/// Query that retrieves the available workspaces (tenants) for the currently authenticated user.
/// </summary>
public sealed record GetMyTenantsQuery : IQuery<IReadOnlyList<TenantSelectionDto>>;

/// <summary>
/// Handles workspace discovery for authenticated multi-tenant users.
/// </summary>
public sealed class GetMyTenantsQueryHandler : IQueryHandler<GetMyTenantsQuery, IReadOnlyList<TenantSelectionDto>>
{
    private readonly ICompanyRepository _companies;
    private readonly ITenantProvider _tenantProvider;
    private readonly IUserRepository _users;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetMyTenantsQueryHandler"/> class.
    /// </summary>
    /// <param name="companies">The company repository.</param>
    /// <param name="users">The user repository.</param>
    /// <param name="tenantProvider">The current tenant provider.</param>
    public GetMyTenantsQueryHandler(
        ICompanyRepository companies,
        IUserRepository users,
        ITenantProvider tenantProvider)
    {
        _companies = companies;
        _users = users;
        _tenantProvider = tenantProvider;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TenantSelectionDto>> HandleAsync(
        GetMyTenantsQuery query,
        CancellationToken cancellationToken = default)
    {
        var currentUser = await _users.GetByIdForAuthenticationAsync(_tenantProvider.CurrentUserId, cancellationToken)
            .ConfigureAwait(false);

        if (currentUser is null)
        {
            throw new UnauthorizedAccessException("Current user not found.");
        }

        var memberships = await _users.ListByEmailForAuthenticationAsync(currentUser.Email, cancellationToken)
            .ConfigureAwait(false);
        var activeMemberships = memberships.Where(u => u.IsActive).ToList();

        var tenants = new List<TenantSelectionDto>();
        foreach (var membership in activeMemberships)
        {
            var company = await _companies.GetByIdForAuthenticationAsync(membership.CompanyId, cancellationToken)
                .ConfigureAwait(false);

            if (company is null || !company.IsActive)
            {
                continue;
            }

            var owner = await _users.GetOwnerByCompanyIdForAuthenticationAsync(company.Id, cancellationToken)
                .ConfigureAwait(false);

            tenants.Add(new TenantSelectionDto(
                company.Id.Value,
                company.Name,
                company.ContactEmail ?? "Unknown",
                owner?.DisplayName ?? "Unknown",
                owner?.Email ?? "Unknown"));
        }

        return tenants;
    }
}
