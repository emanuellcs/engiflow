namespace EngiFlow.Api.Initialization;

/// <summary>
/// Defines the default tenant and administrator account created for local development.
/// </summary>
public sealed class DevelopmentSeedOptions
{
    /// <summary>
    /// Gets the configuration section path for development seed settings.
    /// </summary>
    public const string SectionName = "EngiFlow:DevelopmentSeed";

    /// <summary>
    /// Gets or initializes the default company name.
    /// </summary>
    public string CompanyName { get; init; } = "EngiFlow Demo Company";

    /// <summary>
    /// Gets the default administrator email address.
    /// </summary>
    public string AdminEmail { get; init; } = string.Empty;


    /// <summary>
    /// Gets or initializes the default administrator display name.
    /// </summary>
    public string AdminDisplayName { get; init; } = "EngiFlow Administrator";

    /// <summary>
    /// Gets or initializes the default administrator password.
    /// </summary>
    public string AdminPassword { get; init; } = "EngiFlow_Admin_123!";
}
