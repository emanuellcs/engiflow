namespace EngiFlow.Domain.Users;

/// <summary>
/// Defines the user-facing purpose of a password setup token.
/// </summary>
public enum PasswordSetupTokenPurpose
{
    /// <summary>
    /// The token activates a pending invitation.
    /// </summary>
    Invitation,

    /// <summary>
    /// The token allows an active user to reset their password.
    /// </summary>
    Reset
}
