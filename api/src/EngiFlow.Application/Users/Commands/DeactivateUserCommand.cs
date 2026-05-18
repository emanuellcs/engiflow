using EngiFlow.Application.Abstractions.Cqrs;
using EngiFlow.Application.Abstractions.Persistence;
using EngiFlow.Application.Abstractions.Tenancy;
using EngiFlow.Application.Exceptions;
using EngiFlow.Application.Messaging;
using EngiFlow.Application.Users.Notifications;
using EngiFlow.Domain.ValueObjects;
using FluentValidation;

namespace EngiFlow.Application.Users.Commands;

/// <summary>
/// Command that deactivates a tenant user without deleting the row.
/// </summary>
/// <param name="UserId">The target user identifier.</param>
public sealed record DeactivateUserCommand(Guid UserId) : ICommand<bool>;

/// <summary>
/// Validates user deactivation requests.
/// </summary>
public sealed class DeactivateUserCommandValidator : AbstractValidator<DeactivateUserCommand>
{
    /// <summary>
    /// Initializes validation rules for deactivation.
    /// </summary>
    public DeactivateUserCommandValidator()
    {
        RuleFor(command => command.UserId)
            .NotEmpty()
            .WithMessage("User id is required.");
    }
}

/// <summary>
/// Handles soft deletion by deactivating the target user.
/// </summary>
public sealed class DeactivateUserCommandHandler : ICommandHandler<DeactivateUserCommand, bool>
{
    private readonly ITenantProvider _tenantProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPostCommitNotificationQueue _notifications;
    private readonly IUserRepository _users;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeactivateUserCommandHandler"/> class.
    /// </summary>
    public DeactivateUserCommandHandler(
        IUserRepository users,
        IUnitOfWork unitOfWork,
        ITenantProvider tenantProvider,
        IPostCommitNotificationQueue notifications)
    {
        _users = users;
        _unitOfWork = unitOfWork;
        _tenantProvider = tenantProvider;
        _notifications = notifications;
    }

    /// <inheritdoc />
    public async Task<bool> HandleAsync(
        DeactivateUserCommand command,
        CancellationToken cancellationToken = default)
    {
        var actor = await UserManagementRules.GetActiveCurrentUserAsync(
                _users,
                _tenantProvider,
                cancellationToken)
            .ConfigureAwait(false);
        var target = await _users.GetByIdAsync(UserId.From(command.UserId), cancellationToken)
            .ConfigureAwait(false);

        if (target is null)
        {
            throw new EntityNotFoundException("User", command.UserId);
        }

        UserManagementRules.EnsureCanDeactivateTarget(actor, target);
        target.Deactivate();
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _notifications.EnqueueUserDeactivated(target);

        return true;
    }
}
