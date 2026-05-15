using EngiFlow.Domain.Users;

namespace EngiFlow.Application.Abstractions.Security;

/// <summary>
/// Issues authenticated access tokens for EngiFlow users.
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Creates an access token containing the user's identity, tenant, and role claims.
    /// </summary>
    /// <param name="user">The authenticated user for whom a token should be issued.</param>
    /// <returns>The serialized token and its expiration timestamp.</returns>
    AccessTokenResult CreateAccessToken(User user);
}
