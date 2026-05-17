using EngiFlow.Domain.Users;
using EngiFlow.Domain.ValueObjects;

namespace EngiFlow.Application.Abstractions.Persistence;

/// <summary>
/// Provides tenant-scoped persistence operations for workflow users.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Stages a new tenant-scoped user for insertion.
    /// </summary>
    /// <param name="user">The user to persist.</param>
    /// <param name="cancellationToken">A token that can cancel the persistence operation.</param>
    Task AddAsync(User user, CancellationToken cancellationToken = default);

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

    /// <summary>
    /// Lists active users within the current tenant.
    /// </summary>
    /// <param name="cancellationToken">A token that can cancel the persistence operation.</param>
    /// <returns>The active users visible in the current tenant boundary.</returns>
    Task<IReadOnlyList<User>> ListActiveAsync(CancellationToken cancellationToken = default);
}
