using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace EngiFlow.Api.Hubs;

/// <summary>
/// SignalR hub for real-time user security enforcement.
/// </summary>
[Authorize]
public sealed class SecurityHub : Hub<ISecurityClient>
{
}
