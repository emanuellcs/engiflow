namespace EngiFlow.Domain.Users;

/// <summary>
/// Defines the lifecycle status of a tenant-scoped user account.
/// </summary>
public enum UserStatus
{
    /// <summary>
    /// The user has been invited but has not set an initial password.
    /// </summary>
    PendingActivation,

    /// <summary>
    /// The user can authenticate and perform authorized tenant actions.
    /// </summary>
    Active,

    /// <summary>
    /// The user has been administratively disabled without deleting history.
    /// </summary>
    Deactivated
}
