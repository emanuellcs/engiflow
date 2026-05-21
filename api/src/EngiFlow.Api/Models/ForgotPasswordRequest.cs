namespace EngiFlow.Api.Models;

/// <summary>
/// Request body used to accept a forgot-password reset link request.
/// </summary>
/// <param name="Email">The account email address.</param>
/// <param name="TenantId">The optional tenant identifier for multi-tenant accounts.</param>
public sealed record ForgotPasswordRequest(string Email, Guid? TenantId = null);
