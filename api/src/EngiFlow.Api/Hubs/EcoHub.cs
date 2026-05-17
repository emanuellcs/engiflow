using EngiFlow.Api.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace EngiFlow.Api.Hubs;

/// <summary>
/// SignalR hub for tenant-scoped ECO real-time updates.
/// </summary>
[Authorize]
public sealed class EcoHub : Hub<IEcoClient>
{
    /// <summary>
    /// Creates the SignalR group name for a tenant.
    /// </summary>
    /// <param name="companyId">The tenant identifier.</param>
    /// <returns>The SignalR group name.</returns>
    public static string TenantGroupName(Guid companyId)
    {
        return $"tenant:{companyId:D}";
    }

    /// <inheritdoc />
    public override async Task OnConnectedAsync()
    {
        var tenantClaim = Context.User?.FindFirst(EngiFlowClaimTypes.Tenant)?.Value;
        if (!Guid.TryParse(tenantClaim, out var companyId) || companyId == Guid.Empty)
        {
            Context.Abort();
            return;
        }

        await Groups.AddToGroupAsync(
                Context.ConnectionId,
                TenantGroupName(companyId),
                Context.ConnectionAborted)
            .ConfigureAwait(false);

        await base.OnConnectedAsync().ConfigureAwait(false);
    }
}
