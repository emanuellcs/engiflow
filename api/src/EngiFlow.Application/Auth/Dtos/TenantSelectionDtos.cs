namespace EngiFlow.Application.Auth.Dtos;

/// <summary>
/// Describes a tenant option returned during pre-authentication workspace selection.
/// </summary>
/// <param name="TenantId">The tenant identifier.</param>
/// <param name="CompanyName">The tenant display name.</param>
/// <param name="CompanyEmail">The tenant contact email.</param>
/// <param name="OwnerName">The tenant owner display name.</param>
/// <param name="OwnerEmail">The tenant owner email.</param>
public sealed record TenantSelectionDto(
    Guid TenantId,
    string CompanyName,
    string CompanyEmail,
    string OwnerName,
    string OwnerEmail);

/// <summary>
/// Describes the login response, either a final bearer session or a tenant selection challenge.
/// </summary>
/// <param name="RequiresTenantSelection">Whether the client must select a tenant before JWT issuance.</param>
/// <param name="AccessToken">The final bearer token when no tenant selection is needed.</param>
/// <param name="TokenType">The token type used in the Authorization header.</param>
/// <param name="ExpiresAtUtc">The bearer token expiration timestamp.</param>
/// <param name="UserName">The authenticated user's display name.</param>
/// <param name="CompanyName">The authenticated tenant display name.</param>
/// <param name="Roles">The authenticated user's role names.</param>
/// <param name="PreAuthToken">The short-lived pre-authentication token for tenant selection.</param>
/// <param name="PreAuthExpiresAtUtc">The pre-authentication token expiration timestamp.</param>
/// <param name="Tenants">The tenant choices available to the verified credentials.</param>
public sealed record LoginResponseDto(
    bool RequiresTenantSelection,
    string? AccessToken,
    string? TokenType,
    DateTimeOffset? ExpiresAtUtc,
    string? UserName,
    string? CompanyName,
    IReadOnlyList<string> Roles,
    string? PreAuthToken,
    DateTimeOffset? PreAuthExpiresAtUtc,
    IReadOnlyList<TenantSelectionDto> Tenants)
{
    /// <summary>
    /// Creates a final login response from an issued bearer session.
    /// </summary>
    /// <param name="result">The issued login result.</param>
    /// <returns>A response containing the final bearer token.</returns>
    public static LoginResponseDto Authenticated(LoginResultDto result)
    {
        return new LoginResponseDto(
            false,
            result.AccessToken,
            result.TokenType,
            result.ExpiresAtUtc,
            result.UserName,
            result.CompanyName,
            result.Roles,
            null,
            null,
            []);
    }

    /// <summary>
    /// Creates a tenant selection challenge response.
    /// </summary>
    /// <param name="preAuthToken">The short-lived pre-authentication token.</param>
    /// <param name="expiresAtUtc">The token expiration timestamp.</param>
    /// <param name="tenants">The available tenant choices.</param>
    /// <returns>A response that instructs the client to select a tenant.</returns>
    public static LoginResponseDto TenantSelection(
        string preAuthToken,
        DateTimeOffset expiresAtUtc,
        IReadOnlyList<TenantSelectionDto> tenants)
    {
        return new LoginResponseDto(
            true,
            null,
            null,
            null,
            null,
            null,
            [],
            preAuthToken,
            expiresAtUtc,
            tenants);
    }
}
