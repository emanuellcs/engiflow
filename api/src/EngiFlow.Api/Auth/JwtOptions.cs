namespace EngiFlow.Api.Auth;

/// <summary>
/// Configuration settings used to issue and validate EngiFlow JWT bearer tokens.
/// </summary>
public sealed class JwtOptions
{
    /// <summary>
    /// Gets the configuration section path for JWT settings.
    /// </summary>
    public const string SectionName = "EngiFlow:Authentication:Jwt";

    /// <summary>
    /// Gets or initializes the expected token issuer.
    /// </summary>
    public string Issuer { get; init; } = string.Empty;

    /// <summary>
    /// Gets or initializes the expected token audience.
    /// </summary>
    public string Audience { get; init; } = string.Empty;

    /// <summary>
    /// Gets or initializes the symmetric signing key used for HMAC token signing.
    /// </summary>
    public string SigningKey { get; init; } = string.Empty;

    /// <summary>
    /// Gets or initializes the access token lifetime in minutes.
    /// </summary>
    public int AccessTokenMinutes { get; init; } = 60;

    /// <summary>
    /// Validates that required JWT settings are present and secure enough for HMAC signing.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when required settings are missing or invalid.</exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Issuer))
        {
            throw new InvalidOperationException($"{SectionName}:Issuer is required.");
        }

        if (string.IsNullOrWhiteSpace(Audience))
        {
            throw new InvalidOperationException($"{SectionName}:Audience is required.");
        }

        if (string.IsNullOrWhiteSpace(SigningKey) || SigningKey.Length < 32)
        {
            throw new InvalidOperationException($"{SectionName}:SigningKey must be at least 32 characters.");
        }

        if (AccessTokenMinutes <= 0)
        {
            throw new InvalidOperationException($"{SectionName}:AccessTokenMinutes must be greater than zero.");
        }
    }
}
