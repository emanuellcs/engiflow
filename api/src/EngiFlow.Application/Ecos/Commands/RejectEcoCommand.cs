using EngiFlow.Application.Abstractions.Cqrs;
using EngiFlow.Application.Abstractions.Persistence;
using EngiFlow.Application.Abstractions.Tenancy;
using EngiFlow.Application.Ecos.Dtos;
using EngiFlow.Application.Exceptions;
using EngiFlow.Domain.ValueObjects;
using FluentValidation;

namespace EngiFlow.Application.Ecos.Commands;

/// <summary>
/// Command that rejects an ECO currently under review.
/// </summary>
/// <param name="EcoId">The identifier of the ECO to reject.</param>
/// <param name="Reason">The business justification for rejecting the ECO.</param>
public sealed record RejectEcoCommand(Guid EcoId, string Reason) : ICommand<EcoDetailsDto>;

/// <summary>
/// Validates <see cref="RejectEcoCommand"/> requests before rejection handlers execute.
/// </summary>
public sealed class RejectEcoCommandValidator : AbstractValidator<RejectEcoCommand>
{
    /// <summary>
    /// Initializes validation rules for rejecting an ECO.
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
/// Handles ECO rejection by invoking the aggregate-owned rejection transition.
/// </summary>
public sealed class RejectEcoCommandHandler : ICommandHandler<RejectEcoCommand, EcoDetailsDto>
{
    private readonly IEngineeringChangeOrderRepository _ecos;
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
        IUnitOfWork unitOfWork,
        ITenantProvider tenantProvider)
    {
        _ecos = ecos;
        _users = users;
        _unitOfWork = unitOfWork;
        _tenantProvider = tenantProvider;
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

        eco.Reject(actorUserId, command.Reason);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

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
