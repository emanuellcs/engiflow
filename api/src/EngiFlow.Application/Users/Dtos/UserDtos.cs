namespace EngiFlow.Application.Users.Dtos;

/// <summary>
/// Describes a tenant-scoped user in administrator team management views.
/// </summary>
/// <param name="Id">The user's unique identifier.</param>
/// <param name="Name">The user's display name.</param>
/// <param name="Email">The user's email address.</param>
/// <param name="Role">The user's role name.</param>
/// <param name="LastLoginAt">The last successful login timestamp, when available.</param>
public sealed record UserSummaryDto(
    Guid Id,
    string Name,
    string Email,
    string Role,
    DateTimeOffset? LastLoginAt = null);
