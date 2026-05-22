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
        return _dbContext.Users
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(
                user => user.Id == id && user.CompanyId == _dbContext.CurrentCompanyId,
                cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<User>> ListByEmailForAuthenticationAsync(
        string normalizedEmail,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .IgnoreQueryFilters()
            .Where(user => user.Email == normalizedEmail)
            .OrderBy(user => user.CompanyId)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<User?> GetByIdForAuthenticationAsync(UserId id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Users
            .IgnoreQueryFilters()
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
            .Where(user => user.Id == id && user.Status == UserStatus.Active)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(user => user.LastLoginAt, lastLoginAt),
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<User>> ListActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .Where(user => user.Status == UserStatus.Active)
            .OrderBy(user => user.DisplayName)
            .ThenBy(user => user.Email)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SetPasswordAndActivateAsync(
        UserId id,
        string passwordHash,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Users
            .IgnoreQueryFilters()
            .Where(user => user.Id == id && user.Status != UserStatus.Deactivated)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(user => user.PasswordHash, passwordHash)
                    .SetProperty(user => user.Status, UserStatus.Active)
                    .SetProperty(user => user.DeactivatedAt, (DateTimeOffset?)null),
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<User>> ListForAdministrationAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .IgnoreQueryFilters()
            .Where(user => user.CompanyId == _dbContext.CurrentCompanyId)
            .OrderBy(user => user.DisplayName)
            .ThenBy(user => user.Email)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<User>> SearchAsync(
        string term,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var pattern = $"%{term}%";
        var query = _dbContext.Users.AsQueryable();

        if (_dbContext.Database.IsNpgsql())
        {
            query = query.Where(user =>
                EF.Functions.ILike(user.DisplayName, pattern) ||
                EF.Functions.ILike(user.Email, pattern));
        }
        else
        {
            query = query.Where(user =>
                user.DisplayName.Contains(term) ||
                user.Email.Contains(term));
        }

        return await query
            .OrderBy(user => user.DisplayName)
            .Take(limit)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<int> CountAsync(UserStatus? status = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Users.AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(user => user.Status == status.Value);
        }

        return await query.CountAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<User?> GetOwnerByCompanyIdForAuthenticationAsync(
        CompanyId companyId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                user => user.CompanyId == companyId && user.Role == UserRole.Owner,
                cancellationToken);
    }
}
