using EngiFlow.Domain.Companies;
using EngiFlow.Domain.ValueObjects;

namespace EngiFlow.Application.Abstractions.Persistence;

/// <summary>
/// Provides tenant-scoped persistence operations for company workflow settings.
/// </summary>
public interface ICompanySettingsRepository
{
    /// <summary>
    /// Finds company settings by tenant identifier.
    /// </summary>
    /// <param name="companyId">The company tenant identifier.</param>
    /// <param name="cancellationToken">A token that can cancel the persistence operation.</param>
    /// <returns>The settings when found; otherwise, <see langword="null"/>.</returns>
    Task<CompanySettings?> GetByCompanyIdAsync(CompanyId companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stages settings for insertion.
    /// </summary>
    /// <param name="settings">The settings to persist.</param>
    /// <param name="cancellationToken">A token that can cancel the persistence operation.</param>
    Task AddAsync(CompanySettings settings, CancellationToken cancellationToken = default);
}
