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
/// Command that creates a new engineering change order in draft status.
/// </summary>
/// <param name="Title">The short business title of the requested engineering change.</param>
/// <param name="Description">The detailed engineering change description.</param>
/// <param name="Priority">The operational priority assigned to the request.</param>
public sealed record CreateEcoCommand(
    string Title,
    string Description,
    EcoPriority Priority) : ICommand<EcoDetailsDto>;

/// <summary>
/// Validates <see cref="CreateEcoCommand"/> requests before draft ECO creation.
/// </summary>
public sealed class CreateEcoCommandValidator : AbstractValidator<CreateEcoCommand>
{
    /// <summary>
    /// Initializes validation rules for creating a draft ECO.
    /// </summary>
    public CreateEcoCommandValidator()
    {
        RuleFor(command => command.Title)
            .NotEmpty()
            .WithMessage("Title is required.")
            .MaximumLength(200)
            .WithMessage("Title cannot exceed 200 characters.");

        RuleFor(command => command.Description)
            .NotEmpty()
            .WithMessage("Description is required.")
            .MaximumLength(4_000)
            .WithMessage("Description cannot exceed 4000 characters.");

        RuleFor(command => command.Priority)
            .IsInEnum()
            .WithMessage("Priority is invalid.");
    }
}

/// <summary>
/// Handles draft ECO creation and persists the aggregate-created audit event.
/// </summary>
public sealed class CreateEcoCommandHandler : ICommandHandler<CreateEcoCommand, EcoDetailsDto>
{
    private readonly IEngineeringChangeOrderRepository _ecos;
    private readonly IPostCommitNotificationQueue _notifications;
    private readonly ITenantProvider _tenantProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _users;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateEcoCommandHandler"/> class.
    /// </summary>
    /// <param name="ecos">The ECO repository.</param>
    /// <param name="users">The user repository.</param>
    /// <param name="unitOfWork">The unit of work used to save the ECO and audit event.</param>
    /// <param name="tenantProvider">The current tenant and actor provider.</param>
    public CreateEcoCommandHandler(
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
        CreateEcoCommand command,
        CancellationToken cancellationToken = default)
    {
        var actorUserId = await EnsureCurrentUserCanActAsync(cancellationToken).ConfigureAwait(false);
        var eco = EngineeringChangeOrder.Create(
            _tenantProvider.CurrentCompanyId,
            command.Title,
            command.Description,
            command.Priority,
            actorUserId);

        await _ecos.AddAsync(eco, cancellationToken).ConfigureAwait(false);
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
