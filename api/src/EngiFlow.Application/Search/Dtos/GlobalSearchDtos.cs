using EngiFlow.Domain.Ecos;
using EngiFlow.Domain.Users;

namespace EngiFlow.Application.Search.Dtos;

/// <summary>
/// Aggregated search results containing both ECOs and Team Members.
/// </summary>
/// <param name="Ecos">The list of matching engineering change orders.</param>
/// <param name="Users">The list of matching team members.</param>
public sealed record GlobalSearchResultDto(
    IReadOnlyList<EcoSearchResultDto> Ecos,
    IReadOnlyList<UserSearchResultDto> Users);

/// <summary>
/// A summary of an engineering change order found via global search.
/// </summary>
/// <param name="Id">The unique ECO identifier.</param>
/// <param name="Title">The business title of the ECO.</param>
/// <param name="Description">The detailed description of the ECO.</param>
/// <param name="Status">The current status of the ECO.</param>
public sealed record EcoSearchResultDto(
    Guid Id,
    string Title,
    string Description,
    EcoStatus Status);

/// <summary>
/// A summary of a team member found via global search.
/// </summary>
/// <param name="Id">The unique user identifier.</param>
/// <param name="DisplayName">The user's full display name.</param>
/// <param name="Email">The user's email address.</param>
/// <param name="Role">The user's system role.</param>
public sealed record UserSearchResultDto(
    Guid Id,
    string DisplayName,
    string Email,
    UserRole Role);
