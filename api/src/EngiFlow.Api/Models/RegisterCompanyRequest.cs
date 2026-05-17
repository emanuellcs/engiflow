namespace EngiFlow.Api.Models;

/// <summary>
/// Request body used to create a new company tenant and first administrator account.
/// </summary>
/// <param name="CompanyName">The company display name for the new tenant.</param>
/// <param name="AdminName">The first administrator's full display name.</param>
/// <param name="AdminEmail">The first administrator's email address.</param>
/// <param name="AdminPassword">The first administrator's plain-text password.</param>
public sealed record RegisterCompanyRequest(
    string CompanyName,
    string AdminName,
    string AdminEmail,
    string AdminPassword);
