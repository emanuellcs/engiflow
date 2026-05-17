using EngiFlow.Application.Abstractions.Persistence;
using EngiFlow.Domain.Companies;
using EngiFlow.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace EngiFlow.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core repository for tenant-scoped company workflow settings.
/// </summary>
internal sealed class CompanySettingsRepository : ICompanySettingsRepository
{
    private readonly EngiFlowDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompanySettingsRepository"/> class.
    /// </summary>
    /// <param name="dbContext">The EF Core database context.</param>
    public CompanySettingsRepository(EngiFlowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public Task<CompanySettings?> GetByCompanyIdAsync(
        CompanyId companyId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.CompanySettings
            .SingleOrDefaultAsync(settings => settings.CompanyId == companyId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(CompanySettings settings, CancellationToken cancellationToken = default)
    {
        await _dbContext.CompanySettings.AddAsync(settings, cancellationToken).ConfigureAwait(false);
    }
}
