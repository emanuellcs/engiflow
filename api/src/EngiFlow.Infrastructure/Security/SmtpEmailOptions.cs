namespace EngiFlow.Infrastructure.Security;

/// <summary>
/// SMTP configuration for system email delivery.
/// </summary>
public sealed class SmtpEmailOptions
{
    /// <summary>
    /// Gets the configuration section path.
    /// </summary>
    public const string SectionName = "EngiFlow:Email:Smtp";

    /// <summary>
    /// Gets or sets the SMTP host name.
    /// </summary>
    public string Host { get; set; } = "localhost";

    /// <summary>
    /// Gets or sets the SMTP port.
    /// </summary>
    public int Port { get; set; } = 1025;

    /// <summary>
    /// Gets or sets a value indicating whether STARTTLS is required.
    /// </summary>
    public bool UseStartTls { get; set; }

    /// <summary>
    /// Gets or sets the optional SMTP username.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Gets or sets the optional SMTP password.
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Gets or sets the sender email address.
    /// </summary>
    public string FromEmail { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the sender display name.
    /// </summary>
    public string FromName { get; set; } = "EngiFlow";

    /// <summary>
    /// Validates required SMTP options.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Host))
        {
            throw new InvalidOperationException("SMTP host is required.");
        }

        if (Port <= 0)
        {
            throw new InvalidOperationException("SMTP port must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(FromEmail))
        {
            throw new InvalidOperationException("SMTP from email is required.");
        }
    }
}
