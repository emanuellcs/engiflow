using EngiFlow.Application.Abstractions.Persistence;
using EngiFlow.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace EngiFlow.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core repository for password setup and reset token records.
/// </summary>
internal sealed class PasswordSetupTokenRepository : IPasswordSetupTokenRepository
{
    private readonly EngiFlowDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="PasswordSetupTokenRepository"/> class.
    /// </summary>
    /// <param name="dbContext">The EF Core database context.</param>
    public PasswordSetupTokenRepository(EngiFlowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task AddAsync(PasswordSetupToken token, CancellationToken cancellationToken = default)
    {
        await _dbContext.PasswordSetupTokens.AddAsync(token, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<PasswordSetupToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return _dbContext.PasswordSetupTokens
            .SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);
    }
}
