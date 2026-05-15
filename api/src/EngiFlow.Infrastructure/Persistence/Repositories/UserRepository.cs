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
            .SingleOrDefaultAsync(user => user.Email == normalizedEmail, cancellationToken);
    }
}
