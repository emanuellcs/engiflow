namespace EngiFlow.Application.Auth.Dtos;

/// <summary>
/// Describes the authenticated session returned after a successful login.
/// </summary>
/// <param name="AccessToken">The serialized JWT access token.</param>
/// <param name="TokenType">The token type clients should use in the Authorization header.</param>
/// <param name="ExpiresAtUtc">The UTC timestamp when the access token expires.</param>
/// <param name="UserName">The authenticated user's display name.</param>
/// <param name="CompanyName">The authenticated user's company display name.</param>
/// <param name="Roles">The authenticated user's role names.</param>
public sealed record LoginResultDto(
    string AccessToken,
    string TokenType,
    DateTimeOffset ExpiresAtUtc,
    string UserName,
    string CompanyName,
    IReadOnlyList<string> Roles);
