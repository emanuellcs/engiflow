using EngiFlow.Domain.Abstractions;
using EngiFlow.Domain.Exceptions;
using EngiFlow.Domain.Guards;
using EngiFlow.Domain.ValueObjects;

namespace EngiFlow.Domain.Companies;

/// <summary>
/// Tenant-scoped workflow settings that control company-wide ECO policies.
/// </summary>
public sealed class CompanySettings : ITenantScoped
{
    private CompanySettings()
    {
    }

    private CompanySettings(CompanyId companyId, int minApprovalsRequired, DateTimeOffset createdAt)
    {
        CompanyId = companyId;
        MinApprovalsRequired = minApprovalsRequired;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    /// <inheritdoc />
    public CompanyId CompanyId { get; private set; }

    /// <summary>
    /// Gets the number of active review-round approvals required for an ECO to become approved.
    /// </summary>
    public int MinApprovalsRequired { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when the settings row was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when settings were last updated.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// Creates default settings for a tenant.
    /// </summary>
    /// <param name="companyId">The company tenant identifier.</param>
    /// <param name="createdAt">Optional deterministic creation timestamp.</param>
    /// <returns>The default settings row.</returns>
    public static CompanySettings CreateDefault(CompanyId companyId, DateTimeOffset? createdAt = null)
    {
        DomainGuard.AgainstDefault(companyId, nameof(companyId));
        var timestamp = DomainGuard.UtcTimestamp(createdAt);
        return new CompanySettings(companyId, 1, timestamp);
    }

    /// <summary>
    /// Updates the minimum approval quorum.
    /// </summary>
    /// <param name="minApprovalsRequired">The required number of approvals.</param>
    /// <param name="updatedAt">Optional deterministic update timestamp.</param>
    /// <exception cref="DomainException">Thrown when the quorum is less than one.</exception>
    public void SetMinApprovalsRequired(int minApprovalsRequired, DateTimeOffset? updatedAt = null)
    {
        if (minApprovalsRequired < 1)
        {
            throw new DomainException("Minimum approvals required must be at least one.");
        }

        MinApprovalsRequired = minApprovalsRequired;
        UpdatedAt = DomainGuard.UtcTimestamp(updatedAt);
    }
}
