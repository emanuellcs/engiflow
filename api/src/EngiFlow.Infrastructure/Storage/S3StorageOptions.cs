namespace EngiFlow.Infrastructure.Storage;

/// <summary>
/// Configuration for the S3-compatible ECO attachment store.
/// </summary>
public sealed class S3StorageOptions
{
    /// <summary>
    /// Gets the configuration section path.
    /// </summary>
    public const string SectionName = "EngiFlow:Storage:S3";

    /// <summary>
    /// Gets or sets the S3 bucket used for ECO attachments.
    /// </summary>
    public string BucketName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the AWS region used for signing requests.
    /// </summary>
    public string Region { get; set; } = "us-east-1";

    /// <summary>
    /// Gets or sets an optional S3-compatible endpoint URL, such as MinIO.
    /// </summary>
    public string? ServiceUrl { get; set; }

    /// <summary>
    /// Gets or sets the access key.
    /// </summary>
    public string AccessKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the secret key.
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether path-style bucket addressing should be used.
    /// </summary>
    public bool ForcePathStyle { get; set; }

    /// <summary>
    /// Validates required storage options.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(BucketName))
        {
            throw new InvalidOperationException("S3 bucket name is required.");
        }

        if (string.IsNullOrWhiteSpace(Region))
        {
            throw new InvalidOperationException("S3 region is required.");
        }

        if (string.IsNullOrWhiteSpace(AccessKey))
        {
            throw new InvalidOperationException("S3 access key is required.");
        }

        if (string.IsNullOrWhiteSpace(SecretKey))
        {
            throw new InvalidOperationException("S3 secret key is required.");
        }
    }
}
