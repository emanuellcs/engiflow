using EngiFlow.Domain.Companies;
using EngiFlow.Domain.ValueObjects;

namespace EngiFlow.Application.Abstractions.Persistence;

/// <summary>
/// Provides persistence operations for company tenant roots.
/// </summary>
public interface ICompanyRepository
{
    /// <summary>
    /// Finds a company tenant by identifier.
    /// </summary>
    /// <param name="id">The company tenant identifier.</param>
    /// <param name="cancellationToken">A token that can cancel the persistence operation.</param>
    /// <returns>The company when found; otherwise, <see langword="null"/>.</returns>
    Task<Company?> GetByIdAsync(CompanyId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stages a new company tenant for insertion.
    /// </summary>
    /// <param name="company">The company aggregate to persist.</param>
    /// <param name="cancellationToken">A token that can cancel the persistence operation.</param>
    Task AddAsync(Company company, CancellationToken cancellationToken = default);
}
