using EngiFlow.Application.Abstractions.Cqrs;
using EngiFlow.Application.Abstractions.Persistence;
using EngiFlow.Application.Abstractions.Tenancy;
using EngiFlow.Application.Ecos.Dtos;
using EngiFlow.Domain.Companies;

namespace EngiFlow.Application.Ecos.Queries;

/// <summary>
/// Query that retrieves tenant users and quorum settings for ECO review UI.
/// </summary>
public sealed record GetEcoReviewContextQuery : IQuery<EcoReviewContextDto>;

/// <summary>
/// Handles retrieval of PR-like ECO review context for the current tenant.
/// </summary>
public sealed class GetEcoReviewContextQueryHandler : IQueryHandler<GetEcoReviewContextQuery, EcoReviewContextDto>
{
    private readonly ICompanySettingsRepository _settings;
    private readonly ITenantProvider _tenantProvider;
    private readonly IUserRepository _users;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetEcoReviewContextQueryHandler"/> class.
    /// </summary>
    public GetEcoReviewContextQueryHandler(
        IUserRepository users,
        ICompanySettingsRepository settings,
        ITenantProvider tenantProvider)
    {
        _users = users;
        _settings = settings;
        _tenantProvider = tenantProvider;
    }

    /// <inheritdoc />
    public async Task<EcoReviewContextDto> HandleAsync(
        GetEcoReviewContextQuery query,
        CancellationToken cancellationToken = default)
    {
        var settings = await _settings
            .GetByCompanyIdAsync(_tenantProvider.CurrentCompanyId, cancellationToken)
            .ConfigureAwait(false);
        var minApprovalsRequired =
            settings?.MinApprovalsRequired ??
            CompanySettings.CreateDefault(_tenantProvider.CurrentCompanyId).MinApprovalsRequired;
        var users = await _users.ListActiveAsync(cancellationToken).ConfigureAwait(false);

        return new EcoReviewContextDto(
            minApprovalsRequired,
            users
                .Select(user => new EcoUserDto(
                    user.Id.Value,
                    user.DisplayName,
                    user.Email,
                    user.Role.ToString()))
                .ToArray());
    }
}
