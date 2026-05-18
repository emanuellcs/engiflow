using EngiFlow.Application.Abstractions.Persistence;
using EngiFlow.Domain.Users;
using EngiFlow.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace EngiFlow.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core repository for tenant-scoped workflow users.
/// </summary>
internal sealed class UserRepository : IUserRepository
{
    private readonly EngiFlowDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserRepository"/> class.
    /// </summary>
    /// <param name="dbContext">The EF Core database context.</param>
    public UserRepository(EngiFlowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await _dbContext.Users.AddAsync(user, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Users.SingleOrDefaultAsync(user => user.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public Task<User?> GetByEmailForAuthenticationAsync(
        string normalizedEmail,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleOrDefaultAsync(user => user.Email == normalizedEmail, cancellationToken);
    }

    /// <inheritdoc />
    public Task<User?> GetByIdForAuthenticationAsync(UserId id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleOrDefaultAsync(user => user.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task RecordSuccessfulLoginAsync(
        UserId id,
        DateTimeOffset lastLoginAt,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Users
            .IgnoreQueryFilters()
            .Where(user => user.Id == id && user.IsActive)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(user => user.LastLoginAt, lastLoginAt),
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<User>> ListActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .Where(user => user.IsActive)
            .OrderBy(user => user.DisplayName)
            .ThenBy(user => user.Email)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
