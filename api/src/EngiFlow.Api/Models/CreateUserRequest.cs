using EngiFlow.Domain.Users;

namespace EngiFlow.Api.Models;

/// <summary>
/// HTTP request body used by administrators to create a tenant-scoped user.
/// </summary>
/// <param name="Name">The user's display name.</param>
/// <param name="Email">The user's email address.</param>
/// <param name="Password">The user's initial plain-text password.</param>
/// <param name="Role">The user's initial role.</param>
public sealed record CreateUserRequest(
    string Name,
    string Email,
    string Password,
    UserRole Role);
