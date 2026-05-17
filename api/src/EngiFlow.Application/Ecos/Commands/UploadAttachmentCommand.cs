using EngiFlow.Application.Abstractions.Cqrs;
using EngiFlow.Application.Abstractions.Persistence;
using EngiFlow.Application.Abstractions.Storage;
using EngiFlow.Application.Abstractions.Tenancy;
using EngiFlow.Application.Ecos.Dtos;
using EngiFlow.Application.Ecos.Notifications;
using EngiFlow.Application.Messaging;
using FluentValidation;

namespace EngiFlow.Application.Ecos.Commands;

/// <summary>
/// Command that uploads a file to object storage and records ECO attachment metadata.
/// </summary>
public sealed record UploadAttachmentCommand(
    Guid EcoId,
    string FileName,
    string ContentType,
    long ContentLength,
    Stream Content) : ICommand<EcoDetailsDto>;

/// <summary>
/// Validates <see cref="UploadAttachmentCommand"/> requests.
/// </summary>
public sealed class UploadAttachmentCommandValidator : AbstractValidator<UploadAttachmentCommand>
{
    /// <summary>
    /// Initializes validation rules for ECO attachment upload requests.
    /// </summary>
    public UploadAttachmentCommandValidator()
    {
        RuleFor(command => command.EcoId).NotEmpty().WithMessage("ECO id is required.");
        RuleFor(command => command.FileName)
            .NotEmpty()
            .WithMessage("File name is required.")
            .MaximumLength(255)
            .WithMessage("File name cannot exceed 255 characters.");
        RuleFor(command => command.ContentType)
            .NotEmpty()
            .WithMessage("Content type is required.")
            .MaximumLength(255)
            .WithMessage("Content type cannot exceed 255 characters.");
        RuleFor(command => command.ContentLength)
            .GreaterThan(0)
            .WithMessage("File size must be greater than zero.");
        RuleFor(command => command.Content)
            .NotNull()
            .WithMessage("File content is required.");
    }
}

/// <summary>
/// Handles S3-first attachment uploads with rollback compensation for failed database writes.
/// </summary>
public sealed class UploadAttachmentCommandHandler : ICommandHandler<UploadAttachmentCommand, EcoDetailsDto>
{
    private readonly IExternalOperationCompensation _compensation;
    private readonly IEngineeringChangeOrderRepository _ecos;
    private readonly IPostCommitNotificationQueue _notifications;
    private readonly IStorageService _storage;
    private readonly ITenantProvider _tenantProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _users;

    /// <summary>
    /// Initializes a new instance of the <see cref="UploadAttachmentCommandHandler"/> class.
    /// </summary>
    public UploadAttachmentCommandHandler(
        IEngineeringChangeOrderRepository ecos,
        IUserRepository users,
        IStorageService storage,
        IUnitOfWork unitOfWork,
        ITenantProvider tenantProvider,
        IPostCommitNotificationQueue notifications,
        IExternalOperationCompensation compensation)
    {
        _ecos = ecos;
        _users = users;
        _storage = storage;
        _unitOfWork = unitOfWork;
        _tenantProvider = tenantProvider;
        _notifications = notifications;
        _compensation = compensation;
    }

    /// <inheritdoc />
    public async Task<EcoDetailsDto> HandleAsync(
        UploadAttachmentCommand command,
        CancellationToken cancellationToken = default)
    {
        var actorUserId = await EcoCommandHandlerSupport
            .EnsureCurrentUserCanActAsync(_users, _tenantProvider, cancellationToken)
            .ConfigureAwait(false);
        var eco = await EcoCommandHandlerSupport.GetEcoAsync(_ecos, command.EcoId, cancellationToken)
            .ConfigureAwait(false);

        var upload = await _storage.UploadAsync(
                new StorageUploadRequest(
                    _tenantProvider.CurrentCompanyId.Value,
                    command.EcoId,
                    command.FileName,
                    command.ContentType,
                    command.ContentLength,
                    command.Content),
                cancellationToken)
            .ConfigureAwait(false);

        _compensation.Register(token => _storage.DeleteAsync(upload.ObjectKey, token));

        eco.AddAttachment(
            upload.FileName,
            upload.FileSize,
            upload.ObjectKey,
            upload.MimeType,
            actorUserId);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _notifications.EnqueueEcoChanged(eco);

        return eco.ToDetailsDto();
    }
}
