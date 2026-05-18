using EngiFlow.Application.Abstractions.Cqrs;
using EngiFlow.Application.Abstractions.Persistence;
using EngiFlow.Application.Abstractions.Tenancy;
using EngiFlow.Application.Ecos.Dtos;
using EngiFlow.Application.Ecos.Notifications;
using EngiFlow.Application.Exceptions;
using EngiFlow.Application.Messaging;
using EngiFlow.Domain.Ecos;
using EngiFlow.Domain.ValueObjects;
using FluentValidation;

namespace EngiFlow.Application.Ecos.Commands;

/// <summary>
/// Compatibility command that requests changes on an ECO currently under review.
/// </summary>
/// <param name="EcoId">The identifier of the ECO to return to draft.</param>
/// <param name="Reason">The business justification for returning the ECO to draft.</param>
public sealed record RejectEcoCommand(Guid EcoId, string Reason) : ICommand<EcoDetailsDto>;

/// <summary>
/// Validates <see cref="RejectEcoCommand"/> requests before request-changes handlers execute.
/// </summary>
public sealed class RejectEcoCommandValidator : AbstractValidator<RejectEcoCommand>
{
    /// <summary>
    /// Initializes validation rules for requesting changes on an ECO.
    /// </summary>
    public RejectEcoCommandValidator()
    {
        RuleFor(command => command.EcoId)
            .NotEmpty()
            .WithMessage("ECO id is required.");

        RuleFor(command => command.Reason)
            .NotEmpty()
            .WithMessage("Rejection reason is required.")
            .MaximumLength(500)
            .WithMessage("Rejection reason cannot exceed 500 characters.");
    }
}

/// <summary>
/// Handles legacy rejection requests by invoking the aggregate-owned request-changes transition.
/// </summary>
public sealed class RejectEcoCommandHandler : ICommandHandler<RejectEcoCommand, EcoDetailsDto>
{
    private readonly IEngineeringChangeOrderRepository _ecos;
    private readonly IPostCommitNotificationQueue _notifications;
    private readonly ICompanySettingsRepository _settings;
    private readonly ITenantProvider _tenantProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _users;

    /// <summary>
    /// Initializes a new instance of the <see cref="RejectEcoCommandHandler"/> class.
    /// </summary>
    /// <param name="ecos">The ECO repository.</param>
    /// <param name="users">The user repository.</param>
    /// <param name="unitOfWork">The unit of work used to save the transition and audit event.</param>
    /// <param name="tenantProvider">The current tenant and actor provider.</param>
    public RejectEcoCommandHandler(
        IEngineeringChangeOrderRepository ecos,
        IUserRepository users,
        ICompanySettingsRepository settings,
        IUnitOfWork unitOfWork,
        ITenantProvider tenantProvider,
        IPostCommitNotificationQueue notifications)
    {
        _ecos = ecos;
        _users = users;
        _settings = settings;
        _unitOfWork = unitOfWork;
        _tenantProvider = tenantProvider;
        _notifications = notifications;
    }

    /// <inheritdoc />
    public async Task<EcoDetailsDto> HandleAsync(
        RejectEcoCommand command,
        CancellationToken cancellationToken = default)
    {
        var actorUserId = await EnsureCurrentUserCanActAsync(cancellationToken).ConfigureAwait(false);
        var eco = await _ecos.GetByIdWithEventsAsync(
                EngineeringChangeOrderId.From(command.EcoId),
                cancellationToken)
            .ConfigureAwait(false);

        if (eco is null)
        {
            throw new EntityNotFoundException("EngineeringChangeOrder", command.EcoId);
        }

        var minApprovalsRequired = await EcoCommandHandlerSupport
            .GetMinApprovalsRequiredAsync(_settings, _tenantProvider, cancellationToken)
            .ConfigureAwait(false);

        eco.SubmitReviewDecision(
            actorUserId,
            EcoApprovalDecision.RequestChanges,
            minApprovalsRequired,
            command.Reason);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _notifications.EnqueueEcoChanged(eco);

        return eco.ToDetailsDto();
    }

    private async Task<UserId> EnsureCurrentUserCanActAsync(CancellationToken cancellationToken)
    {
        var actorUserId = _tenantProvider.CurrentUserId;
        var actor = await _users.GetByIdAsync(actorUserId, cancellationToken).ConfigureAwait(false);

        if (actor is null)
        {
            throw new EntityNotFoundException("User", actorUserId.Value);
        }

        actor.EnsureActive();
        return actorUserId;
    }
}
