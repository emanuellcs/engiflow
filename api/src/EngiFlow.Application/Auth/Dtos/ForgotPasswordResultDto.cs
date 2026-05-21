namespace EngiFlow.Application.Auth.Dtos;

/// <summary>
/// Describes the result of a forgot-password request, potentially requiring tenant selection.
/// </summary>
/// <param name="RequiresTenantSelection">Whether the client must select a tenant before the email is sent.</param>
/// <param name="Tenants">The tenant choices available for the provided email.</param>
public sealed record ForgotPasswordResultDto(
    bool RequiresTenantSelection,
    IReadOnlyList<TenantSelectionDto> Tenants)
{
    /// <summary>
    /// Creates a successful forgot-password result where the email was dispatched.
    /// </summary>
    /// <returns>A successful result.</returns>
    public static ForgotPasswordResultDto Success() => new(false, []);

    /// <summary>
    /// Creates a tenant selection challenge for multi-tenant users.
    /// </summary>
    /// <param name="tenants">The available tenant choices.</param>
    /// <returns>A tenant selection challenge result.</returns>
    public static ForgotPasswordResultDto Challenge(IReadOnlyList<TenantSelectionDto> tenants) => new(true, tenants);
}
