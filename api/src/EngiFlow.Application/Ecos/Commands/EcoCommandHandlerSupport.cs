using EngiFlow.Application.Abstractions.Persistence;
using EngiFlow.Application.Abstractions.Tenancy;
using EngiFlow.Application.Exceptions;
using EngiFlow.Domain.Companies;
using EngiFlow.Domain.Ecos;
using EngiFlow.Domain.ValueObjects;

namespace EngiFlow.Application.Ecos.Commands;

/// <summary>
/// Shared guard helpers for ECO command handlers.
/// </summary>
internal static class EcoCommandHandlerSupport
{
    /// <summary>
    /// Ensures the current actor is an active tenant user.
    /// </summary>
    public static async Task<UserId> EnsureCurrentUserCanActAsync(
        IUserRepository users,
        ITenantProvider tenantProvider,
        CancellationToken cancellationToken)
    {
        var actorUserId = tenantProvider.CurrentUserId;
        var actor = await users.GetByIdAsync(actorUserId, cancellationToken).ConfigureAwait(false);

        if (actor is null)
        {
            throw new EntityNotFoundException("User", actorUserId.Value);
        }

        actor.EnsureActive();
        return actorUserId;
    }

    /// <summary>
    /// Loads an ECO with its workflow child collections.
    /// </summary>
    public static async Task<EngineeringChangeOrder> GetEcoAsync(
        IEngineeringChangeOrderRepository ecos,
        Guid ecoId,
        CancellationToken cancellationToken)
    {
        var eco = await ecos.GetByIdWithEventsAsync(
                EngineeringChangeOrderId.From(ecoId),
                cancellationToken)
            .ConfigureAwait(false);

        if (eco is null)
        {
            throw new EntityNotFoundException("EngineeringChangeOrder", ecoId);
        }

        return eco;
    }

    /// <summary>
    /// Gets the tenant approval quorum, creating default settings for older tenants when needed.
    /// </summary>
    public static async Task<int> GetMinApprovalsRequiredAsync(
        ICompanySettingsRepository settingsRepository,
        ITenantProvider tenantProvider,
        CancellationToken cancellationToken)
    {
        var companyId = tenantProvider.CurrentCompanyId;
        var settings = await settingsRepository.GetByCompanyIdAsync(companyId, cancellationToken)
            .ConfigureAwait(false);

        if (settings is not null)
        {
            return settings.MinApprovalsRequired;
        }

        settings = CompanySettings.CreateDefault(companyId);
        await settingsRepository.AddAsync(settings, cancellationToken).ConfigureAwait(false);
        return settings.MinApprovalsRequired;
    }
}
