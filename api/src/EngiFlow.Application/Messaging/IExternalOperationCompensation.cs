namespace EngiFlow.Application.Messaging;

/// <summary>
/// Tracks external operations that must be compensated if the current command transaction fails.
/// </summary>
public interface IExternalOperationCompensation
{
    /// <summary>
    /// Registers an asynchronous compensation callback.
    /// </summary>
    /// <param name="compensation">The callback to execute when the command fails.</param>
    void Register(Func<CancellationToken, Task> compensation);

    /// <summary>
    /// Runs registered compensations in reverse registration order.
    /// </summary>
    /// <param name="cancellationToken">A token that can cancel compensation work.</param>
    Task CompensateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all registered compensation callbacks without executing them.
    /// </summary>
    void Clear();
}

/// <summary>
/// Scoped in-memory compensation queue for one command execution.
/// </summary>
internal sealed class ExternalOperationCompensation : IExternalOperationCompensation
{
    private readonly List<Func<CancellationToken, Task>> _compensations = [];

    /// <inheritdoc />
    public void Register(Func<CancellationToken, Task> compensation)
    {
        ArgumentNullException.ThrowIfNull(compensation);
        _compensations.Add(compensation);
    }

    /// <inheritdoc />
    public async Task CompensateAsync(CancellationToken cancellationToken = default)
    {
        for (var index = _compensations.Count - 1; index >= 0; index--)
        {
            await _compensations[index](cancellationToken).ConfigureAwait(false);
        }

        _compensations.Clear();
    }

    /// <inheritdoc />
    public void Clear()
    {
        _compensations.Clear();
    }
}
