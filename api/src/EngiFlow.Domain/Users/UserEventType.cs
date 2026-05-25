namespace EngiFlow.Domain.Users;

/// <summary>
/// Classifies user lifecycle events that must be retained for audit review.
/// </summary>
public enum UserEventType
{
    /// <summary>
    /// An administrator invited a user to activate an account.
    /// </summary>
    UserInvited,

    /// <summary>
    /// A pending user activated their account by setting a password.
    /// </summary>
    UserActivated,

    /// <summary>
    /// An administrator deactivated a user account.
    /// </summary>
    UserDeactivated,

    /// <summary>
    /// An administrator reactivated a previously deactivated account.
    /// </summary>
    UserReactivated,

    /// <summary>
    /// A password reset token was issued.
    /// </summary>
    PasswordResetRequested,

    /// <summary>
    /// A first-access activation token was regenerated for a pending account.
    /// </summary>
    FirstAccessInvitationResent
}
