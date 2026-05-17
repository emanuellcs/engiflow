using EngiFlow.Domain.Companies;

namespace EngiFlow.Application.Abstractions.Persistence;

/// <summary>
/// Provides persistence operations for company tenant roots.
/// </summary>
public interface ICompanyRepository
{
    /// <summary>
    /// Stages a new company tenant for insertion.
    /// </summary>
    /// <param name="company">The company aggregate to persist.</param>
    /// <param name="cancellationToken">A token that can cancel the persistence operation.</param>
    Task AddAsync(Company company, CancellationToken cancellationToken = default);
}
