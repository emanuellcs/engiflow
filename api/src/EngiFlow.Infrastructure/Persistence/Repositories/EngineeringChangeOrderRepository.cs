using EngiFlow.Application.Abstractions.Persistence;
using EngiFlow.Domain.Ecos;
using EngiFlow.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace EngiFlow.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core repository for tenant-scoped engineering change order aggregates.
/// </summary>
internal sealed class EngineeringChangeOrderRepository : IEngineeringChangeOrderRepository
{
    private readonly EngiFlowDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="EngineeringChangeOrderRepository"/> class.
    /// </summary>
    /// <param name="dbContext">The EF Core database context.</param>
    public EngineeringChangeOrderRepository(EngiFlowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task AddAsync(EngineeringChangeOrder eco, CancellationToken cancellationToken = default)
    {
        await _dbContext.EngineeringChangeOrders.AddAsync(eco, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<EngineeringChangeOrder?> GetByIdAsync(
        EngineeringChangeOrderId id,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.EngineeringChangeOrders
            .SingleOrDefaultAsync(eco => eco.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public Task<EngineeringChangeOrder?> GetByIdWithEventsAsync(
        EngineeringChangeOrderId id,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.EngineeringChangeOrders
            .Include(eco => eco.Events)
            .Include(eco => eco.Comments)
            .Include(eco => eco.AffectedItems)
            .Include(eco => eco.Approvals)
            .Include(eco => eco.Attachments)
            .SingleOrDefaultAsync(eco => eco.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<EngineeringChangeOrder>> ListAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.EngineeringChangeOrders
            .AsNoTracking()
            .OrderByDescending(eco => eco.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.EngineeringChangeOrders.CountAsync(cancellationToken);
    }
}
