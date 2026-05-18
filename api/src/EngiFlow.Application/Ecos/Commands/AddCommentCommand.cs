using EngiFlow.Application.Abstractions.Cqrs;
using EngiFlow.Application.Abstractions.Persistence;
using EngiFlow.Application.Abstractions.Tenancy;
using EngiFlow.Application.Ecos.Dtos;
using EngiFlow.Application.Ecos.Notifications;
using EngiFlow.Application.Messaging;
using FluentValidation;

namespace EngiFlow.Application.Ecos.Commands;

/// <summary>
/// Command that appends a user comment to an ECO timeline.
/// </summary>
public sealed record AddCommentCommand(Guid EcoId, string Body) : ICommand<EcoDetailsDto>;

/// <summary>
/// Validates <see cref="AddCommentCommand"/> requests.
/// </summary>
public sealed class AddCommentCommandValidator : AbstractValidator<AddCommentCommand>
{
    /// <summary>
    /// Initializes validation rules for ECO comments.
    /// </summary>
    public AddCommentCommandValidator()
    {
        RuleFor(command => command.EcoId).NotEmpty().WithMessage("ECO id is required.");
        RuleFor(command => command.Body)
            .NotEmpty()
            .WithMessage("Comment body is required.")
            .MaximumLength(4_000)
            .WithMessage("Comment body cannot exceed 4000 characters.");
    }
}

/// <summary>
/// Handles appending comments to ECO timelines.
/// </summary>
public sealed class AddCommentCommandHandler : ICommandHandler<AddCommentCommand, EcoDetailsDto>
{
    private readonly IEngineeringChangeOrderRepository _ecos;
    private readonly IPostCommitNotificationQueue _notifications;
    private readonly ITenantProvider _tenantProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _users;

    /// <summary>
    /// Initializes a new instance of the <see cref="AddCommentCommandHandler"/> class.
    /// </summary>
    public AddCommentCommandHandler(
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
        AddCommentCommand command,
        CancellationToken cancellationToken = default)
    {
        var actorUserId = await EcoCommandHandlerSupport
            .EnsureCurrentUserCanActAsync(_users, _tenantProvider, cancellationToken)
            .ConfigureAwait(false);
        var eco = await EcoCommandHandlerSupport.GetEcoAsync(_ecos, command.EcoId, cancellationToken)
            .ConfigureAwait(false);

        eco.AddComment(command.Body, actorUserId);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _notifications.EnqueueEcoChanged(eco);

        return eco.ToDetailsDto();
    }
}
