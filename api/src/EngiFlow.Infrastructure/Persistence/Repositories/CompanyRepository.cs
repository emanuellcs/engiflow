using EngiFlow.Application.Abstractions.Persistence;
using EngiFlow.Domain.Companies;

namespace EngiFlow.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core repository for company tenant roots.
/// </summary>
internal sealed class CompanyRepository : ICompanyRepository
{
    private readonly EngiFlowDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompanyRepository"/> class.
    /// </summary>
    /// <param name="dbContext">The EF Core database context.</param>
    public CompanyRepository(EngiFlowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task AddAsync(Company company, CancellationToken cancellationToken = default)
    {
        await _dbContext.Companies.AddAsync(company, cancellationToken).ConfigureAwait(false);
    }
}
