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
/// Command that records an approver's decision for the active ECO review round.
/// </summary>
public sealed record SubmitReviewDecisionCommand(
    Guid EcoId,
    EcoApprovalDecision Decision,
    string? Comment) : ICommand<EcoDetailsDto>;

/// <summary>
/// Validates <see cref="SubmitReviewDecisionCommand"/> requests.
/// </summary>
public sealed class SubmitReviewDecisionCommandValidator : AbstractValidator<SubmitReviewDecisionCommand>
{
    /// <summary>
    /// Initializes validation rules for review decisions.
    /// </summary>
    public SubmitReviewDecisionCommandValidator()
    {
        RuleFor(command => command.EcoId).NotEmpty().WithMessage("ECO id is required.");
        RuleFor(command => command.Decision).IsInEnum().WithMessage("Review decision is invalid.");
        RuleFor(command => command.Comment)
            .MaximumLength(4_000)
            .WithMessage("Review comment cannot exceed 4000 characters.");
    }
}

/// <summary>
/// Handles review decisions and quorum transitions.
/// </summary>
public sealed class SubmitReviewDecisionCommandHandler : ICommandHandler<SubmitReviewDecisionCommand, EcoDetailsDto>
{
    private readonly IEngineeringChangeOrderRepository _ecos;
    private readonly IPostCommitNotificationQueue _notifications;
    private readonly ICompanySettingsRepository _settings;
    private readonly ITenantProvider _tenantProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _users;

    /// <summary>
    /// Initializes a new instance of the <see cref="SubmitReviewDecisionCommandHandler"/> class.
    /// </summary>
    public SubmitReviewDecisionCommandHandler(
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
        SubmitReviewDecisionCommand command,
        CancellationToken cancellationToken = default)
    {
        var actorUserId = await EcoCommandHandlerSupport
            .EnsureCurrentUserCanActAsync(_users, _tenantProvider, cancellationToken)
            .ConfigureAwait(false);
        var minApprovalsRequired = await EcoCommandHandlerSupport
            .GetMinApprovalsRequiredAsync(_settings, _tenantProvider, cancellationToken)
            .ConfigureAwait(false);
        var eco = await EcoCommandHandlerSupport.GetEcoAsync(_ecos, command.EcoId, cancellationToken)
            .ConfigureAwait(false);

        eco.SubmitReviewDecision(
            actorUserId,
            command.Decision,
            minApprovalsRequired,
            command.Comment);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _notifications.EnqueueEcoChanged(eco);

        return eco.ToDetailsDto();
    }
}
