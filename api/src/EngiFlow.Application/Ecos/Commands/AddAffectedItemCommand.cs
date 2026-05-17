using EngiFlow.Application.Abstractions.Cqrs;
using EngiFlow.Application.Abstractions.Persistence;
using EngiFlow.Application.Abstractions.Tenancy;
using EngiFlow.Application.Ecos.Dtos;
using EngiFlow.Application.Ecos.Notifications;
using EngiFlow.Application.Messaging;
using EngiFlow.Domain.Ecos;
using FluentValidation;

namespace EngiFlow.Application.Ecos.Commands;

/// <summary>
/// Command that adds an affected engineering item to a draft ECO.
/// </summary>
public sealed record AddAffectedItemCommand(
    Guid EcoId,
    string PartNumber,
    string Description,
    string CurrentRevision,
    string NewRevision,
    EcoAffectedItemAction Action) : ICommand<EcoDetailsDto>;

/// <summary>
/// Validates <see cref="AddAffectedItemCommand"/> requests.
/// </summary>
public sealed class AddAffectedItemCommandValidator : AbstractValidator<AddAffectedItemCommand>
{
    /// <summary>
    /// Initializes validation rules for adding affected items.
    /// </summary>
    public AddAffectedItemCommandValidator()
    {
        RuleFor(command => command.EcoId).NotEmpty().WithMessage("ECO id is required.");
        RuleFor(command => command.PartNumber)
            .NotEmpty()
            .WithMessage("Part number is required.")
            .MaximumLength(100)
            .WithMessage("Part number cannot exceed 100 characters.");
        RuleFor(command => command.Description)
            .NotEmpty()
            .WithMessage("Description is required.")
            .MaximumLength(500)
            .WithMessage("Description cannot exceed 500 characters.");
        RuleFor(command => command.CurrentRevision)
            .NotEmpty()
            .WithMessage("Current revision is required.")
            .MaximumLength(50)
            .WithMessage("Current revision cannot exceed 50 characters.");
        RuleFor(command => command.NewRevision)
            .NotEmpty()
            .WithMessage("New revision is required.")
            .MaximumLength(50)
            .WithMessage("New revision cannot exceed 50 characters.");
        RuleFor(command => command.Action).IsInEnum().WithMessage("Affected item action is invalid.");
    }
}

/// <summary>
/// Handles adding affected items to draft ECOs.
/// </summary>
public sealed class AddAffectedItemCommandHandler : ICommandHandler<AddAffectedItemCommand, EcoDetailsDto>
{
    private readonly IEngineeringChangeOrderRepository _ecos;
    private readonly IPostCommitNotificationQueue _notifications;
    private readonly ITenantProvider _tenantProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _users;

    /// <summary>
    /// Initializes a new instance of the <see cref="AddAffectedItemCommandHandler"/> class.
    /// </summary>
    public AddAffectedItemCommandHandler(
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
        AddAffectedItemCommand command,
        CancellationToken cancellationToken = default)
    {
        var actorUserId = await EcoCommandHandlerSupport
            .EnsureCurrentUserCanActAsync(_users, _tenantProvider, cancellationToken)
            .ConfigureAwait(false);
        var eco = await EcoCommandHandlerSupport.GetEcoAsync(_ecos, command.EcoId, cancellationToken)
            .ConfigureAwait(false);

        eco.AddAffectedItem(
            command.PartNumber,
            command.Description,
            command.CurrentRevision,
            command.NewRevision,
            command.Action,
            actorUserId);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _notifications.EnqueueEcoChanged(eco);

        return eco.ToDetailsDto();
    }
}
