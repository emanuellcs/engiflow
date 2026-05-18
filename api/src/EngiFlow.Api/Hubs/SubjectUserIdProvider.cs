using EngiFlow.Api.Auth;
using Microsoft.AspNetCore.SignalR;

namespace EngiFlow.Api.Hubs;

/// <summary>
/// Uses EngiFlow's JWT subject claim as the SignalR user identifier.
/// </summary>
public sealed class SubjectUserIdProvider : IUserIdProvider
{
    /// <inheritdoc />
    public string? GetUserId(HubConnectionContext connection)
    {
        return connection.User?.FindFirst(EngiFlowClaimTypes.Subject)?.Value;
    }
}
