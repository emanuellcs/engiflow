using EngiFlow.Application.Abstractions.Cqrs;
using EngiFlow.Application.Abstractions.Persistence;
using EngiFlow.Application.Abstractions.Tenancy;
using EngiFlow.Application.Ecos.Dtos;
using EngiFlow.Application.Exceptions;
using EngiFlow.Domain.ValueObjects;
using FluentValidation;

namespace EngiFlow.Application.Ecos.Commands;

/// <summary>
/// Command that submits a draft ECO for formal review.
/// </summary>
/// <param name="EcoId">The identifier of the ECO to submit.</param>
public sealed record SubmitEcoCommand(Guid EcoId) : ICommand<EcoDetailsDto>;

/// <summary>
/// Validates <see cref="SubmitEcoCommand"/> requests before submission handlers execute.
/// </summary>
public sealed class SubmitEcoCommandValidator : AbstractValidator<SubmitEcoCommand>
{
    /// <summary>
    /// Initializes validation rules for submitting an ECO.
    /// </summary>
    public SubmitEcoCommandValidator()
    {
        RuleFor(command => command.EcoId)
            .NotEmpty()
            .WithMessage("ECO id is required.");
    }
}

/// <summary>
/// Handles ECO submission by invoking the aggregate-owned review transition.
/// </summary>
public sealed class SubmitEcoCommandHandler : ICommandHandler<SubmitEcoCommand, EcoDetailsDto>
{
    private readonly IEngineeringChangeOrderRepository _ecos;
    private readonly ITenantProvider _tenantProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _users;

    /// <summary>
    /// Initializes a new instance of the <see cref="SubmitEcoCommandHandler"/> class.
    /// </summary>
    /// <param name="ecos">The ECO repository.</param>
    /// <param name="users">The user repository.</param>
    /// <param name="unitOfWork">The unit of work used to save the transition and audit event.</param>
    /// <param name="tenantProvider">The current tenant and actor provider.</param>
    public SubmitEcoCommandHandler(
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
        SubmitEcoCommand command,
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

        eco.SubmitForReview(actorUserId);
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
