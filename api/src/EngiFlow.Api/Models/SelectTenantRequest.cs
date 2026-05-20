namespace EngiFlow.Api.Models;

/// <summary>
/// Request body used to exchange a pre-auth token and selected tenant for a bearer token.
/// </summary>
/// <param name="PreAuthToken">The short-lived pre-authentication token.</param>
/// <param name="TenantId">The selected tenant identifier.</param>
public sealed record SelectTenantRequest(string PreAuthToken, Guid TenantId);
