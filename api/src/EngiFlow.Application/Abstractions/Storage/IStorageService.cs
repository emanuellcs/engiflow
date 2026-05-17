namespace EngiFlow.Application.Abstractions.Storage;

/// <summary>
/// Stores and retrieves tenant-scoped ECO attachments in an S3-compatible object store.
/// </summary>
public interface IStorageService
{
    /// <summary>
    /// Uploads an object stream after validating attachment security constraints.
    /// </summary>
    /// <param name="request">The object upload request.</param>
    /// <param name="cancellationToken">A token that can cancel the upload.</param>
    /// <returns>The stored object metadata used by the database record.</returns>
    Task<StorageUploadResult> UploadAsync(
        StorageUploadRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an object by key. Missing objects should be treated as successfully deleted.
    /// </summary>
    /// <param name="objectKey">The object key to delete.</param>
    /// <param name="cancellationToken">A token that can cancel the delete operation.</param>
    Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a short-lived download URL for an existing object key.
    /// </summary>
    /// <param name="objectKey">The object key to sign.</param>
    /// <param name="expiresIn">The URL lifetime.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A pre-signed URL for reading the object.</returns>
    Task<string> GeneratePreSignedUrlAsync(
        string objectKey,
        TimeSpan expiresIn,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Upload request for a validated ECO attachment object.
/// </summary>
/// <param name="CompanyId">The tenant identifier used in the object key.</param>
/// <param name="EcoId">The ECO identifier used in the object key.</param>
/// <param name="FileName">The original client file name.</param>
/// <param name="ContentType">The client supplied MIME type.</param>
/// <param name="ContentLength">The stream length in bytes.</param>
/// <param name="Content">The readable object content stream.</param>
public sealed record StorageUploadRequest(
    Guid CompanyId,
    Guid EcoId,
    string FileName,
    string ContentType,
    long ContentLength,
    Stream Content);

/// <summary>
/// Metadata returned after a successful object upload.
/// </summary>
/// <param name="ObjectKey">The object key inside the configured bucket.</param>
/// <param name="FileName">The normalized original file name.</param>
/// <param name="FileSize">The uploaded object size.</param>
/// <param name="MimeType">The validated MIME type.</param>
public sealed record StorageUploadResult(
    string ObjectKey,
    string FileName,
    long FileSize,
    string MimeType);
