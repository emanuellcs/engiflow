using EngiFlow.Domain.ValueObjects;

namespace EngiFlow.Application.Abstractions.Security;

/// <summary>
/// Issues and validates short-lived pre-authentication tenant selection tokens.
/// </summary>
public interface IPreAuthTokenService
{
    /// <summary>
    /// Creates a signed token that allows tenant selection for a verified email.
    /// </summary>
    /// <param name="normalizedEmail">The normalized verified email address.</param>
    /// <param name="tenantIds">The allowed tenant identifiers.</param>
    /// <param name="lifetime">The token lifetime.</param>
    /// <returns>The signed token and its expiration timestamp.</returns>
    PreAuthTokenResult CreatePreAuthToken(
        string normalizedEmail,
        IReadOnlyCollection<CompanyId> tenantIds,
        TimeSpan lifetime);

    /// <summary>
    /// Validates a signed pre-auth token.
    /// </summary>
    /// <param name="token">The token supplied by the client.</param>
    /// <returns>The validated token payload.</returns>
    PreAuthTokenPayload ValidatePreAuthToken(string token);
}

/// <summary>
/// Represents an issued pre-authentication token.
/// </summary>
/// <param name="Token">The signed token value.</param>
/// <param name="ExpiresAtUtc">The UTC timestamp when the token expires.</param>
public sealed record PreAuthTokenResult(string Token, DateTimeOffset ExpiresAtUtc);

/// <summary>
/// Represents a validated pre-authentication token payload.
/// </summary>
/// <param name="NormalizedEmail">The verified normalized email address.</param>
/// <param name="TenantIds">The tenant identifiers allowed by the token.</param>
/// <param name="ExpiresAtUtc">The UTC timestamp when the token expires.</param>
public sealed record PreAuthTokenPayload(
    string NormalizedEmail,
    IReadOnlyCollection<CompanyId> TenantIds,
    DateTimeOffset ExpiresAtUtc);
