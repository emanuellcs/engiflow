using EngiFlow.Domain.ValueObjects;

namespace EngiFlow.Infrastructure.Tenancy;

/// <summary>
/// Provides a fixed tenant identifier for local development and design-time tooling.
/// </summary>
/// <remarks>
/// This mock provider exists until authentication supplies tenant identity from a
/// verified JWT. A single, documented fallback keeps migrations and local startup
/// deterministic while preserving the same infrastructure contract used in production.
/// </remarks>
public sealed class StaticTenantProvider : ITenantProvider
{
    /// <summary>
    /// Gets the deterministic tenant used when no configuration value is supplied.
    /// </summary>
    public static readonly CompanyId DevelopmentFallbackCompanyId =
        CompanyId.From(Guid.Parse("11111111-1111-1111-1111-111111111111"));

    /// <summary>
    /// Initializes a new instance of the <see cref="StaticTenantProvider"/> class.
    /// </summary>
    /// <param name="currentCompanyId">The non-empty tenant identifier to expose.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="currentCompanyId"/> is the default value.</exception>
    public StaticTenantProvider(CompanyId currentCompanyId)
    {
        if (currentCompanyId == default)
        {
            throw new ArgumentException("A tenant company id is required.", nameof(currentCompanyId));
        }

        CurrentCompanyId = currentCompanyId;
    }

    /// <inheritdoc />
    public CompanyId CurrentCompanyId { get; }

    /// <summary>
    /// Creates a tenant identifier from configuration text, falling back to the development tenant when missing.
    /// </summary>
    /// <param name="value">A configuration value expected to contain a GUID.</param>
    /// <returns>The parsed tenant identifier, or <see cref="DevelopmentFallbackCompanyId"/> when no value is configured.</returns>
    /// <exception cref="InvalidOperationException">Thrown when a configured value is present but is not a valid non-empty GUID.</exception>
    public static CompanyId FromConfigurationValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return DevelopmentFallbackCompanyId;
        }

        if (!Guid.TryParse(value, out var companyId) || companyId == Guid.Empty)
        {
            throw new InvalidOperationException("EngiFlow:Tenancy:CurrentCompanyId must be a non-empty GUID.");
        }

        return CompanyId.From(companyId);
    }
}
