using EngiFlow.Domain.Users;

namespace EngiFlow.Application.Abstractions.Security;

/// <summary>
/// Hashes and verifies user passwords without exposing hashing implementation details to use cases.
/// </summary>
public interface IPasswordHashService
{
    /// <summary>
    /// Produces a secure password hash suitable for storing on the user record.
    /// </summary>
    /// <param name="user">The user whose password is being hashed.</param>
    /// <param name="password">The plain-text password supplied through a trusted write path.</param>
    /// <returns>The opaque password hash.</returns>
    string HashPassword(User user, string password);

    /// <summary>
    /// Verifies a supplied password against the hash currently stored on the user.
    /// </summary>
    /// <param name="user">The user being authenticated.</param>
    /// <param name="password">The plain-text password supplied by the login request.</param>
    /// <returns><see langword="true"/> when the password is valid; otherwise, <see langword="false"/>.</returns>
    bool VerifyPassword(User user, string password);
}
