using EngiFlow.Application.Abstractions.Persistence;

namespace EngiFlow.Infrastructure.Persistence.Repositories;

/// <summary>
/// Entity Framework Core implementation of the application unit-of-work contract.
/// </summary>
internal sealed class EfUnitOfWork : IUnitOfWork
{
    private readonly EngiFlowDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="EfUnitOfWork"/> class.
    /// </summary>
    /// <param name="dbContext">The EF Core database context.</param>
    public EfUnitOfWork(EngiFlowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
