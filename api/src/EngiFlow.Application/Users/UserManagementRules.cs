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
    /// <summary>
    /// Gets the current active actor from tenant context.
    /// </summary>
    /// <param name="users">The user repository.</param>
    /// <param name="tenantProvider">The tenant context provider.</param>
    /// <param name="cancellationToken">A token that can cancel the lookup.</param>
    /// <returns>The current active actor.</returns>
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

    /// <summary>
    /// Ensures the actor can manage tenant users.
    /// </summary>
    /// <param name="actor">The current actor.</param>
    public static void EnsureCanManageUsers(User actor)
    {
        if (actor.Role is not (UserRole.Owner or UserRole.Administrator))
        {
            throw new UnauthorizedAccessException("The current user cannot manage tenant users.");
        }
    }

    /// <summary>
    /// Ensures the actor can manage the target user.
    /// </summary>
    /// <param name="actor">The current actor.</param>
    /// <param name="target">The target user.</param>
    public static void EnsureCanManageTarget(User actor, User target)
    {
        EnsureCanManageUsers(actor);

        if (target.Role == UserRole.Owner)
        {
            throw new DomainException("Owner users cannot be managed.");
        }
    }

    /// <summary>
    /// Ensures the actor can change the target role.
    /// </summary>
    /// <param name="actor">The current actor.</param>
    /// <param name="target">The target user.</param>
    /// <param name="nextRole">The requested role.</param>
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

    /// <summary>
    /// Ensures the actor can deactivate the target user.
    /// </summary>
    /// <param name="actor">The current actor.</param>
    /// <param name="target">The target user.</param>
    public static void EnsureCanDeactivateTarget(User actor, User target)
    {
        EnsureCanManageTarget(actor, target);

        if (actor.Id == target.Id)
        {
            throw new DomainException("A user cannot deactivate themselves.");
        }
    }

    /// <summary>
    /// Ensures the actor can reactivate the target user.
    /// </summary>
    /// <param name="actor">The current actor.</param>
    /// <param name="target">The target user.</param>
    public static void EnsureCanReactivateTarget(User actor, User target)
    {
        EnsureCanManageTarget(actor, target);

        if (target.Status != UserStatus.Deactivated)
        {
            throw new DomainException("Only deactivated users can be reactivated.");
        }
    }
}
