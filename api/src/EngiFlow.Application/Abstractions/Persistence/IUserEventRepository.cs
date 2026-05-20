using EngiFlow.Domain.Users;

namespace EngiFlow.Application.Abstractions.Persistence;

/// <summary>
/// Provides append-only persistence operations for user lifecycle audit events.
/// </summary>
public interface IUserEventRepository
{
    /// <summary>
    /// Stages a new user lifecycle audit event for insertion.
    /// </summary>
    /// <param name="userEvent">The audit event to persist.</param>
    /// <param name="cancellationToken">A token that can cancel the persistence operation.</param>
    Task AddAsync(UserEvent userEvent, CancellationToken cancellationToken = default);
}
