using EngiFlow.Application.Abstractions.Cqrs;
using EngiFlow.Application.Abstractions.Persistence;
using EngiFlow.Application.Abstractions.Storage;
using EngiFlow.Application.Ecos.Dtos;
using EngiFlow.Application.Exceptions;
using EngiFlow.Domain.ValueObjects;
using FluentValidation;

namespace EngiFlow.Application.Ecos.Queries;

/// <summary>
/// Query that creates a short-lived download URL for one ECO attachment.
/// </summary>
/// <param name="EcoId">The ECO identifier.</param>
/// <param name="AttachmentId">The attachment identifier.</param>
public sealed record GetEcoAttachmentDownloadUrlQuery(Guid EcoId, Guid AttachmentId)
    : IQuery<EcoAttachmentDownloadDto>;

/// <summary>
/// Validates attachment download URL requests.
/// </summary>
public sealed class GetEcoAttachmentDownloadUrlQueryValidator
    : AbstractValidator<GetEcoAttachmentDownloadUrlQuery>
{
    /// <summary>
    /// Initializes validation rules for attachment download URL requests.
    /// </summary>
    public GetEcoAttachmentDownloadUrlQueryValidator()
    {
        RuleFor(query => query.EcoId).NotEmpty().WithMessage("ECO id is required.");
        RuleFor(query => query.AttachmentId).NotEmpty().WithMessage("Attachment id is required.");
    }
}

/// <summary>
/// Handles attachment download URL generation after tenant-scoped ECO ownership checks.
/// </summary>
public sealed class GetEcoAttachmentDownloadUrlQueryHandler
    : IQueryHandler<GetEcoAttachmentDownloadUrlQuery, EcoAttachmentDownloadDto>
{
    private static readonly TimeSpan DownloadUrlLifetime = TimeSpan.FromMinutes(10);

    private readonly IEngineeringChangeOrderRepository _ecos;
    private readonly IStorageService _storage;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetEcoAttachmentDownloadUrlQueryHandler"/> class.
    /// </summary>
    public GetEcoAttachmentDownloadUrlQueryHandler(
        IEngineeringChangeOrderRepository ecos,
        IStorageService storage)
    {
        _ecos = ecos;
        _storage = storage;
    }

    /// <inheritdoc />
    public async Task<EcoAttachmentDownloadDto> HandleAsync(
        GetEcoAttachmentDownloadUrlQuery query,
        CancellationToken cancellationToken = default)
    {
        var eco = await _ecos.GetByIdWithEventsAsync(
                EngineeringChangeOrderId.From(query.EcoId),
                cancellationToken)
            .ConfigureAwait(false);

        if (eco is null)
        {
            throw new EntityNotFoundException("EngineeringChangeOrder", query.EcoId);
        }

        var attachmentId = EcoAttachmentId.From(query.AttachmentId);
        var attachment = eco.Attachments.SingleOrDefault(item => item.Id == attachmentId);
        if (attachment is null)
        {
            throw new EntityNotFoundException("EcoAttachment", query.AttachmentId);
        }

        var expiresAtUtc = DateTimeOffset.UtcNow.Add(DownloadUrlLifetime);
        var url = await _storage
            .GeneratePreSignedUrlAsync(attachment.ObjectKey, DownloadUrlLifetime, cancellationToken)
            .ConfigureAwait(false);

        return new EcoAttachmentDownloadDto(url, expiresAtUtc);
    }
}
