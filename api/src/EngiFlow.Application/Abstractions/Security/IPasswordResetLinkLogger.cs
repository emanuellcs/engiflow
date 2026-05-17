namespace EngiFlow.Application.Abstractions.Security;

/// <summary>
/// Logs or delivers password reset links for the current authentication flow.
/// </summary>
public interface IPasswordResetLinkLogger
{
    /// <summary>
    /// Logs the mock reset link generated for the supplied normalized email address.
    /// </summary>
    /// <param name="normalizedEmail">The normalized account email address.</param>
    /// <param name="resetLink">The mock reset link for the MVP flow.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A completed task when the link has been logged.</returns>
    Task LogMockResetLinkAsync(
        string normalizedEmail,
        string resetLink,
        CancellationToken cancellationToken = default);
}
