using EngiFlow.Application.Abstractions.Cqrs;
using EngiFlow.Application.Abstractions.Persistence;
using EngiFlow.Application.Abstractions.Tenancy;
using EngiFlow.Application.Exceptions;
using EngiFlow.Application.Users.Dtos;
using EngiFlow.Domain.Users;
using EngiFlow.Domain.ValueObjects;
using FluentValidation;

namespace EngiFlow.Application.Users.Commands;

/// <summary>
/// Command that reactivates a deactivated tenant user with an audit reason.
/// </summary>
/// <param name="UserId">The target user identifier.</param>
/// <param name="Reason">The compliance reason for reactivation.</param>
public sealed record ReactivateUserCommand(Guid UserId, string Reason) : ICommand<UserSummaryDto>;

/// <summary>
/// Validates user reactivation requests.
/// </summary>
public sealed class ReactivateUserCommandValidator : AbstractValidator<ReactivateUserCommand>
{
    /// <summary>
    /// Initializes validation rules for reactivation.
    /// </summary>
    public ReactivateUserCommandValidator()
    {
        RuleFor(command => command.UserId)
            .NotEmpty()
            .WithMessage("User id is required.");

        RuleFor(command => command.Reason)
            .NotEmpty()
            .WithMessage("Reason is required.")
            .MaximumLength(1000)
            .WithMessage("Reason cannot exceed 1000 characters.");
    }
}

/// <summary>
/// Handles ISO-compliant user reactivation and audit logging.
/// </summary>
public sealed class ReactivateUserCommandHandler : ICommandHandler<ReactivateUserCommand, UserSummaryDto>
{
    private readonly ITenantProvider _tenantProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserEventRepository _userEvents;
    private readonly IUserRepository _users;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReactivateUserCommandHandler"/> class.
    /// </summary>
    /// <param name="users">The user repository.</param>
    /// <param name="unitOfWork">The unit of work used to save reactivation changes.</param>
    /// <param name="tenantProvider">The current tenant provider.</param>
    /// <param name="userEvents">The user lifecycle audit repository.</param>
    public ReactivateUserCommandHandler(
        IUserRepository users,
        IUnitOfWork unitOfWork,
        ITenantProvider tenantProvider,
        IUserEventRepository userEvents)
    {
        _users = users;
        _unitOfWork = unitOfWork;
        _tenantProvider = tenantProvider;
        _userEvents = userEvents;
    }

    /// <inheritdoc />
    public async Task<UserSummaryDto> HandleAsync(
        ReactivateUserCommand command,
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

        UserManagementRules.EnsureCanReactivateTarget(actor, target);
        target.Activate();
        await _userEvents.AddAsync(
                UserEvent.Create(
                    target.CompanyId,
                    target.Id,
                    actor.Id,
                    UserEventType.UserReactivated,
                    command.Reason),
                cancellationToken)
            .ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return target.ToSummaryDto();
    }
}
