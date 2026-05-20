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
    /// Finds users by normalized email for authentication, independent of the current tenant filter.
    /// </summary>
    /// <param name="normalizedEmail">The normalized email address to authenticate.</param>
    /// <param name="cancellationToken">A token that can cancel the persistence operation.</param>
    /// <returns>The matching users across tenant boundaries.</returns>
    Task<IReadOnlyList<User>> ListByEmailForAuthenticationAsync(
        string normalizedEmail,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a user by identifier for authentication middleware, independent of active-user filters.
    /// </summary>
    /// <param name="id">The user identifier.</param>
    /// <param name="cancellationToken">A token that can cancel the persistence operation.</param>
    /// <returns>The user when found; otherwise, <see langword="null"/>.</returns>
    Task<User?> GetByIdForAuthenticationAsync(UserId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records successful login activity during anonymous authentication.
    /// </summary>
    /// <param name="id">The authenticated user identifier.</param>
    /// <param name="lastLoginAt">The UTC login timestamp to store.</param>
    /// <param name="cancellationToken">A token that can cancel the persistence operation.</param>
    Task RecordSuccessfulLoginAsync(
        UserId id,
        DateTimeOffset lastLoginAt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores a new password hash and activates the user during public setup or reset.
    /// </summary>
    /// <param name="id">The target user identifier.</param>
    /// <param name="passwordHash">The new opaque password hash.</param>
    /// <param name="cancellationToken">A token that can cancel the persistence operation.</param>
    Task SetPasswordAndActivateAsync(
        UserId id,
        string passwordHash,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists active users within the current tenant.
    /// </summary>
    /// <param name="cancellationToken">A token that can cancel the persistence operation.</param>
    /// <returns>The active users visible in the current tenant boundary.</returns>
    Task<IReadOnlyList<User>> ListActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all users within the current tenant for administrator management.
    /// </summary>
    /// <param name="cancellationToken">A token that can cancel the persistence operation.</param>
    /// <returns>All tenant users, including pending and deactivated users.</returns>
    Task<IReadOnlyList<User>> ListForAdministrationAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds the owner user for a tenant regardless of the current tenant filter.
    /// </summary>
    /// <param name="companyId">The company tenant identifier.</param>
    /// <param name="cancellationToken">A token that can cancel the persistence operation.</param>
    /// <returns>The owner user when found; otherwise, <see langword="null"/>.</returns>
    Task<User?> GetOwnerByCompanyIdForAuthenticationAsync(
        CompanyId companyId,
        CancellationToken cancellationToken = default);
}
