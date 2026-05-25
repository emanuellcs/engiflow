using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace EngiFlow.Api.Hubs;

/// <summary>
/// SignalR hub for user-scoped real-time notification streaming.
/// </summary>
[Authorize]
public sealed class NotificationHub : Hub<INotificationClient>
{
}
