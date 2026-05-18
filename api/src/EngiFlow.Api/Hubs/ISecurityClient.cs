namespace EngiFlow.Api.Hubs;

/// <summary>
/// Client contract for real-time security enforcement messages.
/// </summary>
public interface ISecurityClient
{
    /// <summary>
    /// Receives a role-change notification for a user.
    /// </summary>
    /// <param name="userId">The affected user identifier.</param>
    /// <param name="newRole">The user's current role.</param>
    Task UserPermissionsChanged(Guid userId, string newRole);

    /// <summary>
    /// Receives a user deactivation notification.
    /// </summary>
    /// <param name="userId">The affected user identifier.</param>
    Task UserDeactivated(Guid userId);
}
