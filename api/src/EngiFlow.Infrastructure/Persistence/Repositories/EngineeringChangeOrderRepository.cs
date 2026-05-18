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
        EcoListFilter? filter = null,
        CancellationToken cancellationToken = default)
    {
        return await ApplyFilter(_dbContext.EngineeringChangeOrders.AsNoTracking(), filter)
            .AsNoTracking()
            .Include(eco => eco.Approvals)
            .OrderByDescending(eco => eco.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<int> CountAsync(EcoListFilter? filter = null, CancellationToken cancellationToken = default)
    {
        return ApplyFilter(_dbContext.EngineeringChangeOrders.AsNoTracking(), filter)
            .CountAsync(cancellationToken);
    }

    private static IQueryable<EngineeringChangeOrder> ApplyFilter(
        IQueryable<EngineeringChangeOrder> query,
        EcoListFilter? filter)
    {
        if (filter is null)
        {
            return query;
        }

        var normalizedSearch = filter.Search?.Trim();
        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            var pattern = $"%{normalizedSearch}%";
            query = query.Where(eco =>
                EF.Functions.ILike(eco.Title, pattern) ||
                EF.Functions.ILike(eco.Description, pattern));
        }

        if (filter.Status is not null)
        {
            query = query.Where(eco => eco.Status == filter.Status);
        }

        if (filter.Priority is not null)
        {
            query = query.Where(eco => eco.Priority == filter.Priority);
        }

        if (filter.CreatedFrom is not null)
        {
            query = query.Where(eco => eco.CreatedAt >= filter.CreatedFrom);
        }

        if (filter.CreatedTo is not null)
        {
            query = query.Where(eco => eco.CreatedAt <= filter.CreatedTo);
        }

        if (filter.CreatedByUserId is not null)
        {
            var createdByUserId = filter.CreatedByUserId.Value;
            query = query.Where(eco => eco.CreatedByUserId == createdByUserId);
        }

        if (filter.AwaitingReviewByUserId is not null)
        {
            var awaitingReviewByUserId = filter.AwaitingReviewByUserId.Value;
            query = query.Where(eco =>
                eco.Status == EcoStatus.UnderReview &&
                !eco.Approvals.Any(approval =>
                    approval.ApproverUserId == awaitingReviewByUserId &&
                    approval.ReviewRound == eco.ReviewRound));
        }

        return query;
    }
}
