using EngiFlow.Application.Abstractions.Cqrs;
using EngiFlow.Application.Abstractions.Persistence;
using EngiFlow.Application.Abstractions.Tenancy;
using EngiFlow.Application.Settings.Dtos;
using EngiFlow.Domain.Companies;

namespace EngiFlow.Application.Settings.Queries;

/// <summary>
/// Query that returns current tenant workflow governance settings.
/// </summary>
public sealed record GetCompanySettingsQuery : IQuery<CompanySettingsDto>;

/// <summary>
/// Handles tenant settings retrieval, materializing defaults for older tenants when needed.
/// </summary>
public sealed class GetCompanySettingsQueryHandler : IQueryHandler<GetCompanySettingsQuery, CompanySettingsDto>
{
    private readonly ICompanySettingsRepository _settings;
    private readonly ITenantProvider _tenantProvider;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetCompanySettingsQueryHandler"/> class.
    /// </summary>
    public GetCompanySettingsQueryHandler(
        ICompanySettingsRepository settings,
        ITenantProvider tenantProvider,
        IUnitOfWork unitOfWork)
    {
        _settings = settings;
        _tenantProvider = tenantProvider;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<CompanySettingsDto> HandleAsync(
        GetCompanySettingsQuery query,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentCompanyId;
        var settings = await _settings.GetByCompanyIdAsync(companyId, cancellationToken)
            .ConfigureAwait(false);

        if (settings is null)
        {
            settings = CompanySettings.CreateDefault(companyId);
            await _settings.AddAsync(settings, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return settings.ToDto();
    }
}
