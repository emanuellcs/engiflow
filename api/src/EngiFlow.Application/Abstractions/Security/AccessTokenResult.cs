namespace EngiFlow.Application.Abstractions.Security;

/// <summary>
/// Represents an issued access token and its UTC expiration.
/// </summary>
/// <param name="AccessToken">The serialized bearer token returned to the client.</param>
/// <param name="ExpiresAtUtc">The UTC timestamp when the token expires.</param>
public sealed record AccessTokenResult(string AccessToken, DateTimeOffset ExpiresAtUtc);
