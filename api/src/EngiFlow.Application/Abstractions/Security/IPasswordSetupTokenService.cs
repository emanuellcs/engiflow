namespace EngiFlow.Application.Abstractions.Security;

/// <summary>
/// Generates and hashes one-time password setup and reset tokens.
/// </summary>
public interface IPasswordSetupTokenService
{
    /// <summary>
    /// Creates a cryptographically random token value suitable for email links.
    /// </summary>
    /// <returns>The raw token value sent to the user.</returns>
    string GenerateToken();

    /// <summary>
    /// Produces a non-reversible hash for token persistence and lookup.
    /// </summary>
    /// <param name="token">The raw token value from the email link.</param>
    /// <returns>The stable hash used by persistence.</returns>
    string HashToken(string token);
}
