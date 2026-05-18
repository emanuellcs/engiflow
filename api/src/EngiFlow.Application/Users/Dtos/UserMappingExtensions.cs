using EngiFlow.Domain.Users;

namespace EngiFlow.Application.Users.Dtos;

/// <summary>
/// Converts user domain entities to application DTOs.
/// </summary>
internal static class UserMappingExtensions
{
    /// <summary>
    /// Converts a user to a team-management summary.
    /// </summary>
    /// <param name="user">The user to map.</param>
    /// <returns>The user summary DTO.</returns>
    public static UserSummaryDto ToSummaryDto(this User user)
    {
        return new UserSummaryDto(
            user.Id.Value,
            user.DisplayName,
            user.Email,
            user.Role.ToString(),
            user.LastLoginAt);
    }
}
