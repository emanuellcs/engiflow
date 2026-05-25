using EngiFlow.Domain.Users;

namespace EngiFlow.Application.Abstractions.Persistence;

/// <summary>
/// Provides persistence operations for one-time password setup and reset tokens.
/// </summary>
public interface IPasswordSetupTokenRepository
{
    /// <summary>
    /// Stages a new password token for insertion.
    /// </summary>
    /// <param name="token">The token record to persist.</param>
    /// <param name="cancellationToken">A token that can cancel the persistence operation.</param>
    Task AddAsync(PasswordSetupToken token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a token by its non-reversible hash.
    /// </summary>
    /// <param name="tokenHash">The hashed token value.</param>
    /// <param name="cancellationToken">A token that can cancel the persistence operation.</param>
    /// <returns>The token when found; otherwise, <see langword="null"/>.</returns>
    Task<PasswordSetupToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default);
}
