using EngiFlow.Domain.Ecos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EngiFlow.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Persists pending ECO audit events automatically during <c>SaveChanges</c>.
/// </summary>
/// <remarks>
/// EngineeringChangeOrder records domain events when business methods are invoked.
/// The interceptor keeps application code from needing to remember a separate audit
/// insert, while still clearing pending events only after the database save succeeds.
/// This preserves audit durability across transient save failures and retry attempts.
/// </remarks>
public sealed class EcoAuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly List<EngineeringChangeOrder> _aggregatesWithStagedEvents = [];

    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        StagePendingEcoEvents(eventData.Context);
        return result;
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        StagePendingEcoEvents(eventData.Context);
        return new ValueTask<InterceptionResult<int>>(result);
    }

    /// <inheritdoc />
    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        ClearStagedPendingEvents();
        return result;
    }

    /// <inheritdoc />
    public override ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        ClearStagedPendingEvents();
        return new ValueTask<int>(result);
    }

    /// <inheritdoc />
    public override void SaveChangesFailed(DbContextErrorEventData eventData)
    {
        _aggregatesWithStagedEvents.Clear();
    }

    /// <inheritdoc />
    public override Task SaveChangesFailedAsync(
        DbContextErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        _aggregatesWithStagedEvents.Clear();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Adds untracked pending ECO audit events to the current unit of work.
    /// </summary>
    /// <param name="context">The DbContext currently saving changes.</param>
    private void StagePendingEcoEvents(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var aggregates = context.ChangeTracker
            .Entries<EngineeringChangeOrder>()
            .Select(entry => entry.Entity)
            .Where(eco => eco.PendingEvents.Count > 0)
            .ToArray();

        if (aggregates.Length == 0)
        {
            return;
        }

        var trackedEventIds = context.ChangeTracker
            .Entries<EcoEvent>()
            .Select(entry => entry.Entity.Id)
            .ToHashSet();

        foreach (var aggregate in aggregates)
        {
            if (!_aggregatesWithStagedEvents.Contains(aggregate))
            {
                _aggregatesWithStagedEvents.Add(aggregate);
            }

            foreach (var pendingEvent in aggregate.PendingEvents)
            {
                if (trackedEventIds.Add(pendingEvent.Id))
                {
                    context.Set<EcoEvent>().Add(pendingEvent);
                }
            }
        }
    }

    /// <summary>
    /// Clears pending event buffers after EF Core has successfully saved the unit of work.
    /// </summary>
    private void ClearStagedPendingEvents()
    {
        foreach (var aggregate in _aggregatesWithStagedEvents)
        {
            aggregate.ClearPendingEvents();
        }

        _aggregatesWithStagedEvents.Clear();
    }
}
