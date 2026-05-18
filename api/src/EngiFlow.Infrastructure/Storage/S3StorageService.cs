using System.Net;
using System.Globalization;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using EngiFlow.Application.Abstractions.Storage;
using EngiFlow.Domain.Exceptions;
using Microsoft.Extensions.Options;

namespace EngiFlow.Infrastructure.Storage;

/// <summary>
/// S3-compatible implementation of ECO attachment storage.
/// </summary>
internal sealed class S3StorageService : IStorageService
{
    /// <summary>
    /// Maximum supported ECO attachment size in bytes.
    /// </summary>
    public const long MaxAttachmentBytes = 25L * 1024L * 1024L;

    private static readonly IReadOnlyDictionary<string, string[]> AllowedMimeTypesByExtension =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = ["application/pdf"],
            [".png"] = ["image/png"],
            [".jpg"] = ["image/jpeg"],
            [".jpeg"] = ["image/jpeg"],
            [".csv"] = ["text/csv", "application/csv", "application/vnd.ms-excel"],
            [".xlsx"] = ["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"],
            [".step"] = ["application/step", "model/step", "application/octet-stream"],
            [".stp"] = ["application/step", "model/step", "application/octet-stream"],
            [".dwg"] = ["application/acad", "application/x-acad", "application/autocad", "image/vnd.dwg", "application/octet-stream"]
        };

    private readonly IAmazonS3 _s3Client;
    private readonly S3StorageOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="S3StorageService"/> class.
    /// </summary>
    /// <param name="options">The configured S3 storage options.</param>
    public S3StorageService(IOptions<S3StorageOptions> options)
    {
        _options = options.Value;
        _options.Validate();
        _s3Client = CreateClient(_options);
    }

    /// <inheritdoc />
    public async Task<StorageUploadResult> UploadAsync(
        StorageUploadRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateUpload(request);

        var normalizedFileName = NormalizeFileName(request.FileName);
        var normalizedContentType = request.ContentType.Trim();
        var objectKey = CreateObjectKey(request.CompanyId, request.EcoId, normalizedFileName);

        var putRequest = new PutObjectRequest
        {
            BucketName = _options.BucketName,
            Key = objectKey,
            InputStream = request.Content,
            ContentType = normalizedContentType,
            AutoCloseStream = false
        };
        putRequest.Headers.ContentLength = request.ContentLength;

        await _s3Client.PutObjectAsync(putRequest, cancellationToken).ConfigureAwait(false);
        return new StorageUploadResult(
            objectKey,
            normalizedFileName,
            request.ContentLength,
            normalizedContentType);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(objectKey))
        {
            return;
        }

        try
        {
            await _s3Client.DeleteObjectAsync(
                    _options.BucketName,
                    objectKey.Trim(),
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
        }
    }

    /// <inheritdoc />
    public Task<string> GeneratePreSignedUrlAsync(
        string objectKey,
        TimeSpan expiresIn,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(objectKey);

        if (expiresIn <= TimeSpan.Zero)
        {
            throw new DomainException("Pre-signed URL expiration must be greater than zero.");
        }

        var url = _s3Client.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = _options.BucketName,
            Key = objectKey.Trim(),
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(expiresIn)
        });

        return Task.FromResult(url);
    }

    private static IAmazonS3 CreateClient(S3StorageOptions options)
    {
        var config = new AmazonS3Config
        {
            ForcePathStyle = options.ForcePathStyle,
            AuthenticationRegion = options.Region
        };

        if (!string.IsNullOrWhiteSpace(options.ServiceUrl))
        {
            config.ServiceURL = options.ServiceUrl;
        }
        else
        {
            config.RegionEndpoint = RegionEndpoint.GetBySystemName(options.Region);
        }

        return new AmazonS3Client(
            new BasicAWSCredentials(options.AccessKey, options.SecretKey),
            config);
    }

    private static void ValidateUpload(StorageUploadRequest request)
    {
        if (request.CompanyId == Guid.Empty)
        {
            throw new DomainException("Company id is required for attachment upload.");
        }

        if (request.EcoId == Guid.Empty)
        {
            throw new DomainException("ECO id is required for attachment upload.");
        }

        if (request.ContentLength <= 0)
        {
            throw new DomainException("Attachment file size must be greater than zero.");
        }

        if (request.ContentLength > MaxAttachmentBytes)
        {
            throw new DomainException("Attachment file size cannot exceed 25 MB.");
        }

        if (request.Content is null || !request.Content.CanRead)
        {
            throw new DomainException("Attachment content stream must be readable.");
        }

        var fileName = NormalizeFileName(request.FileName);
        var extension = Path.GetExtension(fileName);
        if (!AllowedMimeTypesByExtension.TryGetValue(extension, out var allowedMimeTypes))
        {
            throw new DomainException("Attachment file type is not allowed.");
        }

        var contentType = request.ContentType.Trim();
        if (string.IsNullOrWhiteSpace(contentType)
            || !allowedMimeTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase))
        {
            throw new DomainException("Attachment MIME type is not allowed.");
        }
    }

    private static string CreateObjectKey(Guid companyId, Guid ecoId, string fileName)
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"tenants/{companyId:N}/ecos/{ecoId:N}/attachments/{Guid.NewGuid():N}/{fileName}");
    }

    private static string NormalizeFileName(string fileName)
    {
        var normalized = Path.GetFileName(fileName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new DomainException("Attachment file name is required.");
        }

        foreach (var invalidCharacter in Path.GetInvalidFileNameChars())
        {
            normalized = normalized.Replace(invalidCharacter, '_');
        }

        if (normalized.Length > 255)
        {
            var extension = Path.GetExtension(normalized);
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(normalized);
            var maxNameLength = Math.Max(1, 255 - extension.Length);
            normalized = string.Concat(nameWithoutExtension.AsSpan(0, Math.Min(nameWithoutExtension.Length, maxNameLength)), extension);
        }

        return normalized;
    }
}
