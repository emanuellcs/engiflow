using EngiFlow.Application.Abstractions.Cqrs;
using EngiFlow.Application.Abstractions.Persistence;
using EngiFlow.Application.Abstractions.Tenancy;
using EngiFlow.Application.Ecos.Dtos;
using EngiFlow.Application.Ecos.Notifications;
using EngiFlow.Application.Messaging;
using EngiFlow.Domain.ValueObjects;
using FluentValidation;

namespace EngiFlow.Application.Ecos.Commands;

/// <summary>
/// Command that removes an affected engineering item from a draft ECO.
/// </summary>
public sealed record RemoveAffectedItemCommand(Guid EcoId, Guid AffectedItemId) : ICommand<EcoDetailsDto>;

/// <summary>
/// Validates <see cref="RemoveAffectedItemCommand"/> requests.
/// </summary>
public sealed class RemoveAffectedItemCommandValidator : AbstractValidator<RemoveAffectedItemCommand>
{
    /// <summary>
    /// Initializes validation rules for affected item removal.
    /// </summary>
    public RemoveAffectedItemCommandValidator()
    {
        RuleFor(command => command.EcoId).NotEmpty().WithMessage("ECO id is required.");
        RuleFor(command => command.AffectedItemId).NotEmpty().WithMessage("Affected item id is required.");
    }
}

/// <summary>
/// Handles removing affected items from draft ECOs.
/// </summary>
public sealed class RemoveAffectedItemCommandHandler : ICommandHandler<RemoveAffectedItemCommand, EcoDetailsDto>
{
    private readonly IEngineeringChangeOrderRepository _ecos;
    private readonly IPostCommitNotificationQueue _notifications;
    private readonly ITenantProvider _tenantProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _users;

    /// <summary>
    /// Initializes a new instance of the <see cref="RemoveAffectedItemCommandHandler"/> class.
    /// </summary>
    public RemoveAffectedItemCommandHandler(
        IEngineeringChangeOrderRepository ecos,
        IUserRepository users,
        IUnitOfWork unitOfWork,
        ITenantProvider tenantProvider,
        IPostCommitNotificationQueue notifications)
    {
        _ecos = ecos;
        _users = users;
        _unitOfWork = unitOfWork;
        _tenantProvider = tenantProvider;
        _notifications = notifications;
    }

    /// <inheritdoc />
    public async Task<EcoDetailsDto> HandleAsync(
        RemoveAffectedItemCommand command,
        CancellationToken cancellationToken = default)
    {
        var actorUserId = await EcoCommandHandlerSupport
            .EnsureCurrentUserCanActAsync(_users, _tenantProvider, cancellationToken)
            .ConfigureAwait(false);
        var eco = await EcoCommandHandlerSupport.GetEcoAsync(_ecos, command.EcoId, cancellationToken)
            .ConfigureAwait(false);

        eco.RemoveAffectedItem(EcoAffectedItemId.From(command.AffectedItemId), actorUserId);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _notifications.EnqueueEcoChanged(eco);

        return eco.ToDetailsDto();
    }
}
