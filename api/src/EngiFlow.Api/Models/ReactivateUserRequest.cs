namespace EngiFlow.Api.Models;

/// <summary>
/// Request body used to reactivate a deactivated tenant user.
/// </summary>
/// <param name="Reason">The required audit compliance reason.</param>
public sealed record ReactivateUserRequest(string Reason);
