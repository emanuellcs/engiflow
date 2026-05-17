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
/// Command that updates draft ECO title, description, and priority details.
/// </summary>
public sealed record UpdateEcoDetailsCommand(
    Guid EcoId,
    string Title,
    string Description,
    EcoPriority Priority) : ICommand<EcoDetailsDto>;

/// <summary>
/// Validates <see cref="UpdateEcoDetailsCommand"/> requests.
/// </summary>
public sealed class UpdateEcoDetailsCommandValidator : AbstractValidator<UpdateEcoDetailsCommand>
{
    /// <summary>
    /// Initializes validation rules for detail updates.
    /// </summary>
    public UpdateEcoDetailsCommandValidator()
    {
        RuleFor(command => command.EcoId).NotEmpty().WithMessage("ECO id is required.");
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
        RuleFor(command => command.Priority).IsInEnum().WithMessage("Priority is invalid.");
    }
}

/// <summary>
/// Handles draft ECO detail updates.
/// </summary>
public sealed class UpdateEcoDetailsCommandHandler : ICommandHandler<UpdateEcoDetailsCommand, EcoDetailsDto>
{
    private readonly IEngineeringChangeOrderRepository _ecos;
    private readonly IPostCommitNotificationQueue _notifications;
    private readonly ITenantProvider _tenantProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _users;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateEcoDetailsCommandHandler"/> class.
    /// </summary>
    public UpdateEcoDetailsCommandHandler(
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
        UpdateEcoDetailsCommand command,
        CancellationToken cancellationToken = default)
    {
        var actorUserId = await EcoCommandHandlerSupport
            .EnsureCurrentUserCanActAsync(_users, _tenantProvider, cancellationToken)
            .ConfigureAwait(false);
        var eco = await EcoCommandHandlerSupport.GetEcoAsync(_ecos, command.EcoId, cancellationToken)
            .ConfigureAwait(false);

        eco.UpdateDetails(command.Title, command.Description, command.Priority, actorUserId);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _notifications.EnqueueEcoChanged(eco);

        return eco.ToDetailsDto();
    }
}
