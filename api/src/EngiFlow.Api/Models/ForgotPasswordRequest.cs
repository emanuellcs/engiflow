namespace EngiFlow.Api.Models;

/// <summary>
/// Request body used to accept a forgot-password reset link request.
/// </summary>
/// <param name="Email">The account email address.</param>
public sealed record ForgotPasswordRequest(string Email);
