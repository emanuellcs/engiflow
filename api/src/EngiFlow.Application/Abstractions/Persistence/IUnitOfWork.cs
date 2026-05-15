namespace EngiFlow.Application.Abstractions.Persistence;

/// <summary>
/// Persists all staged application changes in a single unit of work.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Saves all staged changes and audit events.
    /// </summary>
    /// <param name="cancellationToken">A token that can cancel the save operation.</param>
    /// <returns>The number of state entries written by the persistence provider.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
