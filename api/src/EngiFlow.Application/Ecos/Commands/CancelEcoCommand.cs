using EngiFlow.Application.Abstractions.Cqrs;
using EngiFlow.Application.Abstractions.Persistence;
using EngiFlow.Application.Abstractions.Tenancy;
using EngiFlow.Application.Ecos.Dtos;
using EngiFlow.Application.Ecos.Notifications;
using EngiFlow.Application.Messaging;
using FluentValidation;

namespace EngiFlow.Application.Ecos.Commands;

/// <summary>
/// Command that cancels a draft or under-review ECO.
/// </summary>
public sealed record CancelEcoCommand(Guid EcoId) : ICommand<EcoDetailsDto>;

/// <summary>
/// Validates <see cref="CancelEcoCommand"/> requests.
/// </summary>
public sealed class CancelEcoCommandValidator : AbstractValidator<CancelEcoCommand>
{
    /// <summary>
    /// Initializes validation rules for ECO cancellation.
    /// </summary>
    public CancelEcoCommandValidator()
    {
        RuleFor(command => command.EcoId).NotEmpty().WithMessage("ECO id is required.");
    }
}

/// <summary>
/// Handles ECO cancellation.
/// </summary>
public sealed class CancelEcoCommandHandler : ICommandHandler<CancelEcoCommand, EcoDetailsDto>
{
    private readonly IEngineeringChangeOrderRepository _ecos;
    private readonly IPostCommitNotificationQueue _notifications;
    private readonly ITenantProvider _tenantProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _users;

    /// <summary>
    /// Initializes a new instance of the <see cref="CancelEcoCommandHandler"/> class.
    /// </summary>
    public CancelEcoCommandHandler(
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
        CancelEcoCommand command,
        CancellationToken cancellationToken = default)
    {
        var actorUserId = await EcoCommandHandlerSupport
            .EnsureCurrentUserCanActAsync(_users, _tenantProvider, cancellationToken)
            .ConfigureAwait(false);
        var eco = await EcoCommandHandlerSupport.GetEcoAsync(_ecos, command.EcoId, cancellationToken)
            .ConfigureAwait(false);

        eco.Cancel(actorUserId);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _notifications.EnqueueEcoChanged(eco);

        return eco.ToDetailsDto();
    }
}
