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
    /// <param name="cancellationToken">A token that can cancel the send operation.</param>
    Task SendPasswordResetAsync(
        string email,
        string resetLink,
        CancellationToken cancellationToken = default);
}
