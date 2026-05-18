using EngiFlow.Domain.Abstractions;
using EngiFlow.Domain.Exceptions;
using EngiFlow.Domain.Guards;
using EngiFlow.Domain.ValueObjects;

namespace EngiFlow.Domain.Ecos;

/// <summary>
/// S3-compatible object metadata for a file attached to an ECO.
/// </summary>
public sealed class EcoAttachment : ITenantScoped
{
    private EcoAttachment()
    {
    }

    private EcoAttachment(
        EcoAttachmentId id,
        CompanyId companyId,
        EngineeringChangeOrderId engineeringChangeOrderId,
        string fileName,
        long fileSize,
        string objectKey,
        string mimeType,
        UserId uploadedByUserId,
        DateTimeOffset uploadedAt)
    {
        Id = id;
        CompanyId = companyId;
        EngineeringChangeOrderId = engineeringChangeOrderId;
        FileName = fileName;
        FileSize = fileSize;
        ObjectKey = objectKey;
        MimeType = mimeType;
        UploadedByUserId = uploadedByUserId;
        UploadedAt = uploadedAt;
    }

    /// <summary>
    /// Gets the attachment identifier.
    /// </summary>
    public EcoAttachmentId Id { get; private set; }

    /// <inheritdoc />
    public CompanyId CompanyId { get; private set; }

    /// <summary>
    /// Gets the ECO identifier this attachment belongs to.
    /// </summary>
    public EngineeringChangeOrderId EngineeringChangeOrderId { get; private set; }

    /// <summary>
    /// Gets the original file name supplied by the client.
    /// </summary>
    public string FileName { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the file size in bytes.
    /// </summary>
    public long FileSize { get; private set; }

    /// <summary>
    /// Gets the object key inside S3-compatible storage.
    /// </summary>
    public string ObjectKey { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the validated MIME type.
    /// </summary>
    public string MimeType { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the user who uploaded the attachment.
    /// </summary>
    public UserId UploadedByUserId { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when the attachment was uploaded.
    /// </summary>
    public DateTimeOffset UploadedAt { get; private set; }

    internal static EcoAttachment Create(
        CompanyId companyId,
        EngineeringChangeOrderId engineeringChangeOrderId,
        string fileName,
        long fileSize,
        string objectKey,
        string mimeType,
        UserId uploadedByUserId,
        DateTimeOffset? uploadedAt = null)
    {
        DomainGuard.AgainstDefault(companyId, nameof(companyId));
        DomainGuard.AgainstDefault(engineeringChangeOrderId, nameof(engineeringChangeOrderId));
        DomainGuard.AgainstDefault(uploadedByUserId, nameof(uploadedByUserId));

        if (fileSize <= 0)
        {
            throw new DomainException("Attachment file size must be greater than zero.");
        }

        return new EcoAttachment(
            EcoAttachmentId.New(),
            companyId,
            engineeringChangeOrderId,
            DomainGuard.Required(fileName, nameof(fileName), 255),
            fileSize,
            DomainGuard.Required(objectKey, nameof(objectKey), 1_024),
            DomainGuard.Required(mimeType, nameof(mimeType), 255),
            uploadedByUserId,
            DomainGuard.UtcTimestamp(uploadedAt));
    }
}
