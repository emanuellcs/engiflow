using EngiFlow.Application.Abstractions.Tenancy;
using EngiFlow.Domain.ValueObjects;

namespace EngiFlow.Infrastructure.Tenancy;

/// <summary>
/// Provides fixed tenant and actor identifiers for design-time tooling and startup initialization.
/// </summary>
/// <remarks>
/// Request handling uses the API-layer HTTP tenant provider. This provider remains useful
/// where no authenticated HTTP context exists, such as EF Core design-time services,
/// tests, and development database seeding.
/// </remarks>
public sealed class StaticTenantProvider : ITenantProvider
{
    /// <summary>
    /// Gets the deterministic tenant used when no configuration value is supplied.
    /// </summary>
    public static readonly CompanyId DevelopmentFallbackCompanyId =
        CompanyId.From(Guid.Parse("11111111-1111-1111-1111-111111111111"));

    /// <summary>
    /// Gets the deterministic user used when no configuration value is supplied.
    /// </summary>
    public static readonly UserId DevelopmentFallbackUserId =
        UserId.From(Guid.Parse("22222222-2222-2222-2222-222222222222"));

    /// <summary>
    /// Initializes a new instance of the <see cref="StaticTenantProvider"/> class.
    /// </summary>
    /// <param name="currentCompanyId">The non-empty tenant identifier to expose.</param>
    /// <param name="currentUserId">The non-empty user identifier to expose.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="currentCompanyId"/> or <paramref name="currentUserId"/> is the default value.
    /// </exception>
    public StaticTenantProvider(CompanyId currentCompanyId, UserId currentUserId)
    {
        if (currentCompanyId == default)
        {
            throw new ArgumentException("A tenant company id is required.", nameof(currentCompanyId));
        }

        if (currentUserId == default)
        {
            throw new ArgumentException("A current user id is required.", nameof(currentUserId));
        }

        CurrentCompanyId = currentCompanyId;
        CurrentUserId = currentUserId;
    }

    /// <inheritdoc />
    public CompanyId CurrentCompanyId { get; }

    /// <inheritdoc />
    public UserId CurrentUserId { get; }

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

    /// <summary>
    /// Creates a user identifier from configuration text, falling back to the development user when missing.
    /// </summary>
    /// <param name="value">A configuration value expected to contain a GUID.</param>
    /// <returns>The parsed user identifier, or <see cref="DevelopmentFallbackUserId"/> when no value is configured.</returns>
    /// <exception cref="InvalidOperationException">Thrown when a configured value is present but is not a valid non-empty GUID.</exception>
    public static UserId UserIdFromConfigurationValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return DevelopmentFallbackUserId;
        }

        if (!Guid.TryParse(value, out var userId) || userId == Guid.Empty)
        {
            throw new InvalidOperationException("EngiFlow:Tenancy:CurrentUserId must be a non-empty GUID.");
        }

        return UserId.From(userId);
    }
}
