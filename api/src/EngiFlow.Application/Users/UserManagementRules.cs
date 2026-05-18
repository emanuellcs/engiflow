using EngiFlow.Application.Abstractions.Persistence;
using EngiFlow.Application.Abstractions.Tenancy;
using EngiFlow.Application.Exceptions;
using EngiFlow.Domain.Exceptions;
using EngiFlow.Domain.Users;
using EngiFlow.Domain.ValueObjects;

namespace EngiFlow.Application.Users;

/// <summary>
/// Shared user authorization helpers for application command handlers.
/// </summary>
internal static class UserManagementRules
{
    public static async Task<User> GetActiveCurrentUserAsync(
        IUserRepository users,
        ITenantProvider tenantProvider,
        CancellationToken cancellationToken)
    {
        var actorUserId = tenantProvider.CurrentUserId;
        var actor = await users.GetByIdAsync(actorUserId, cancellationToken).ConfigureAwait(false);

        if (actor is null)
        {
            throw new EntityNotFoundException("User", actorUserId.Value);
        }

        actor.EnsureActive();
        return actor;
    }

    public static void EnsureCanManageUsers(User actor)
    {
        if (actor.Role is not (UserRole.Owner or UserRole.Administrator))
        {
            throw new UnauthorizedAccessException("The current user cannot manage tenant users.");
        }
    }

    public static void EnsureCanManageTarget(User actor, User target)
    {
        EnsureCanManageUsers(actor);

        if (target.Role == UserRole.Owner)
        {
            throw new DomainException("Owner users cannot be managed.");
        }
    }

    public static void EnsureCanChangeTargetRole(User actor, User target, UserRole nextRole)
    {
        EnsureCanManageTarget(actor, target);

        if (actor.Id == target.Id)
        {
            throw new DomainException("A user cannot change their own role.");
        }

        if (nextRole == UserRole.Owner)
        {
            throw new DomainException("Users cannot be promoted to Owner.");
        }
    }

    public static void EnsureCanDeactivateTarget(User actor, User target)
    {
        EnsureCanManageTarget(actor, target);

        if (actor.Id == target.Id)
        {
            throw new DomainException("A user cannot deactivate themselves.");
        }
    }
}
