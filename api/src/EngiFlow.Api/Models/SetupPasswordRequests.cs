namespace EngiFlow.Api.Models;

/// <summary>
/// Request body used to resolve public setup-password page context.
/// </summary>
/// <param name="Token">The raw setup or reset token.</param>
/// <param name="Email">The account email associated with the token.</param>
public sealed record SetupPasswordContextRequest(string Token, string Email);

/// <summary>
/// Request body used to complete public password setup or reset.
/// </summary>
/// <param name="Token">The raw setup or reset token.</param>
/// <param name="Email">The account email associated with the token.</param>
/// <param name="Password">The new account password.</param>
public sealed record SetupPasswordRequest(string Token, string Email, string Password);
