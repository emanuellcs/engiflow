namespace EngiFlow.Api.Models;

/// <summary>
/// Request body used to switch the current authenticated session to a different tenant.
/// </summary>
/// <param name="TenantId">The target tenant identifier.</param>
public sealed record SwitchTenantRequest(Guid TenantId);
