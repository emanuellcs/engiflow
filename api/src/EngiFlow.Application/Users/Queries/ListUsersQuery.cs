using EngiFlow.Application.Abstractions.Cqrs;
using EngiFlow.Application.Abstractions.Persistence;
using EngiFlow.Application.Users.Dtos;

namespace EngiFlow.Application.Users.Queries;

/// <summary>
/// Query that lists active users in the current tenant.
/// </summary>
public sealed record ListUsersQuery : IQuery<IReadOnlyList<UserSummaryDto>>;

/// <summary>
/// Handles tenant-scoped user listing for administrators.
/// </summary>
public sealed class ListUsersQueryHandler : IQueryHandler<ListUsersQuery, IReadOnlyList<UserSummaryDto>>
{
    private readonly IUserRepository _users;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListUsersQueryHandler"/> class.
    /// </summary>
    /// <param name="users">The user repository.</param>
    public ListUsersQueryHandler(IUserRepository users)
    {
        _users = users;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<UserSummaryDto>> HandleAsync(
        ListUsersQuery query,
        CancellationToken cancellationToken = default)
    {
        var users = await _users.ListActiveAsync(cancellationToken).ConfigureAwait(false);

        return users.Select(user => user.ToSummaryDto()).ToArray();
    }
}
