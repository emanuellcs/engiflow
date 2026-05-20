using EngiFlow.Application.Abstractions.Persistence;
using EngiFlow.Domain.Users;

namespace EngiFlow.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core repository for user lifecycle audit events.
/// </summary>
internal sealed class UserEventRepository : IUserEventRepository
{
    private readonly EngiFlowDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserEventRepository"/> class.
    /// </summary>
    /// <param name="dbContext">The EF Core database context.</param>
    public UserEventRepository(EngiFlowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task AddAsync(UserEvent userEvent, CancellationToken cancellationToken = default)
    {
        await _dbContext.UserEvents.AddAsync(userEvent, cancellationToken).ConfigureAwait(false);
    }
}
