using EngiFlow.Domain.Users;

namespace EngiFlow.Application.Auth.Dtos;

/// <summary>
/// Describes a valid setup-password token context for the public password page.
/// </summary>
/// <param name="Purpose">The token purpose.</param>
/// <param name="Email">The account email associated with the token.</param>
public sealed record SetupPasswordContextDto(PasswordSetupTokenPurpose Purpose, string Email);

/// <summary>
/// Represents a completed password setup or reset operation.
/// </summary>
public sealed record SetupPasswordResultDto;

/// <summary>
/// Describes the result of a first-access resend request, potentially requiring tenant selection.
/// </summary>
/// <param name="RequiresTenantSelection">Whether the client must select a tenant before the email is sent.</param>
/// <param name="Tenants">The tenant choices available for the provided email.</param>
public sealed record FirstAccessResultDto(
    bool RequiresTenantSelection,
    IReadOnlyList<TenantSelectionDto> Tenants)
{
    /// <summary>
    /// Creates a successful first-access result where the email was dispatched.
    /// </summary>
    /// <returns>A successful result.</returns>
    public static FirstAccessResultDto Success() => new(false, []);

    /// <summary>
    /// Creates a tenant selection challenge for multi-tenant users.
    /// </summary>
    /// <param name="tenants">The available tenant choices.</param>
    /// <returns>A tenant selection challenge result.</returns>
    public static FirstAccessResultDto Challenge(IReadOnlyList<TenantSelectionDto> tenants) => new(true, tenants);
}
