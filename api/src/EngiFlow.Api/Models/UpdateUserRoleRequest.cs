using EngiFlow.Domain.Users;

namespace EngiFlow.Api.Models;

/// <summary>
/// HTTP request body used to change a tenant user's role.
/// </summary>
/// <param name="Role">The replacement role.</param>
public sealed record UpdateUserRoleRequest(UserRole Role);
