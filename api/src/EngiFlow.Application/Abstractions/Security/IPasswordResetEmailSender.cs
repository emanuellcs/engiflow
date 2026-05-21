namespace EngiFlow.Application.Abstractions.Security;

/// <summary>
/// Sends password-reset messages for accepted reset requests.
/// </summary>
public interface IPasswordResetEmailSender
{
    /// <summary>
    /// Sends a password-reset email to the supplied address.
    /// </summary>
    /// <param name="email">The normalized recipient email address.</param>
    /// <param name="resetLink">The absolute reset link to include in the message.</param>
    /// <param name="companyName">The optional workspace display name.</param>
    /// <param name="cancellationToken">A token that can cancel the send operation.</param>
    Task SendPasswordResetAsync(
        string email,
        string resetLink,
        string? companyName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a password setup email for a newly invited or pending user.
    /// </summary>
    /// <param name="email">The normalized recipient email address.</param>
    /// <param name="setupLink">The absolute setup link to include in the message.</param>
    /// <param name="companyName">The tenant display name.</param>
    /// <param name="invitedByName">The administrator who issued or resent the invitation.</param>
    /// <param name="cancellationToken">A token that can cancel the send operation.</param>
    Task SendPasswordSetupAsync(
        string email,
        string setupLink,
        string companyName,
        string invitedByName,
        CancellationToken cancellationToken = default);
}
