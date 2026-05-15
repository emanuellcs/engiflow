namespace EngiFlow.Application.Auth.Dtos;

/// <summary>
/// Describes the bearer token returned after a successful login.
/// </summary>
/// <param name="AccessToken">The serialized JWT access token.</param>
/// <param name="TokenType">The token type clients should use in the Authorization header.</param>
/// <param name="ExpiresAtUtc">The UTC timestamp when the access token expires.</param>
public sealed record LoginResultDto(
    string AccessToken,
    string TokenType,
    DateTimeOffset ExpiresAtUtc);
