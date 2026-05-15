using EngiFlow.Domain.Users;
using EngiFlow.Domain.ValueObjects;

namespace EngiFlow.Application.Abstractions.Persistence;

/// <summary>
/// Provides tenant-scoped persistence operations for workflow users.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Finds a user by identifier within the current tenant.
    /// </summary>
    /// <param name="id">The user identifier.</param>
    /// <param name="cancellationToken">A token that can cancel the persistence operation.</param>
    /// <returns>The user when found; otherwise, <see langword="null"/>.</returns>
    Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a user by normalized email for authentication, independent of the current tenant filter.
    /// </summary>
    /// <param name="normalizedEmail">The normalized email address to authenticate.</param>
    /// <param name="cancellationToken">A token that can cancel the persistence operation.</param>
    /// <returns>The user when found; otherwise, <see langword="null"/>.</returns>
    Task<User?> GetByEmailForAuthenticationAsync(
        string normalizedEmail,
        CancellationToken cancellationToken = default);
}
