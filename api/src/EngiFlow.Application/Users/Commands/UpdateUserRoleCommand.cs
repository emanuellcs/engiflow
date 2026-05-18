using EngiFlow.Application.Abstractions.Cqrs;
using EngiFlow.Application.Abstractions.Persistence;
using EngiFlow.Application.Abstractions.Tenancy;
using EngiFlow.Application.Exceptions;
using EngiFlow.Application.Users.Dtos;
using EngiFlow.Application.Messaging;
using EngiFlow.Application.Users.Notifications;
using EngiFlow.Domain.Users;
using EngiFlow.Domain.ValueObjects;
using FluentValidation;

namespace EngiFlow.Application.Users.Commands;

/// <summary>
/// Command that changes a tenant user's role.
/// </summary>
/// <param name="UserId">The target user identifier.</param>
/// <param name="Role">The new role.</param>
public sealed record UpdateUserRoleCommand(Guid UserId, UserRole Role) : ICommand<UserSummaryDto>;

/// <summary>
/// Validates role update requests.
/// </summary>
public sealed class UpdateUserRoleCommandValidator : AbstractValidator<UpdateUserRoleCommand>
{
    /// <summary>
    /// Initializes validation rules for role updates.
    /// </summary>
    public UpdateUserRoleCommandValidator()
    {
        RuleFor(command => command.UserId)
            .NotEmpty()
            .WithMessage("User id is required.");

        RuleFor(command => command.Role)
            .Must(role => role is UserRole.Administrator or UserRole.Approver or UserRole.Requester or UserRole.Viewer)
            .WithMessage("Role must be Administrator, Approver, Requester, or Viewer.");
    }
}

/// <summary>
/// Handles tenant user role updates while enforcing inter-management rules.
/// </summary>
public sealed class UpdateUserRoleCommandHandler : ICommandHandler<UpdateUserRoleCommand, UserSummaryDto>
{
    private readonly ITenantProvider _tenantProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPostCommitNotificationQueue _notifications;
    private readonly IUserRepository _users;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateUserRoleCommandHandler"/> class.
    /// </summary>
    public UpdateUserRoleCommandHandler(
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
    public async Task<UserSummaryDto> HandleAsync(
        UpdateUserRoleCommand command,
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

        UserManagementRules.EnsureCanChangeTargetRole(actor, target, command.Role);
        target.ChangeRole(command.Role);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _notifications.EnqueueUserPermissionsChanged(target);

        return target.ToSummaryDto();
    }
}
