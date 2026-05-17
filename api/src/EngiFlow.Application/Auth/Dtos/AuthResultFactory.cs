using EngiFlow.Application.Abstractions.Security;
using EngiFlow.Domain.Companies;
using EngiFlow.Domain.Users;

namespace EngiFlow.Application.Auth.Dtos;

/// <summary>
/// Creates authenticated-session DTOs from issued tokens and domain identity data.
/// </summary>
internal static class AuthResultFactory
{
    /// <summary>
    /// Builds a login result for the supplied user and company.
    /// </summary>
    /// <param name="token">The issued bearer token.</param>
    /// <param name="user">The authenticated user.</param>
    /// <param name="company">The user's active tenant company.</param>
    /// <returns>The enriched login result returned to clients.</returns>
    public static LoginResultDto Create(
        AccessTokenResult token,
        User user,
        Company company)
    {
        return new LoginResultDto(
            token.AccessToken,
            "Bearer",
            token.ExpiresAtUtc,
            user.DisplayName,
            company.Name,
            [user.Role.ToString()]);
    }
}
