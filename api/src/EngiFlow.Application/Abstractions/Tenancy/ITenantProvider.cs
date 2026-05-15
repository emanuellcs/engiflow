using EngiFlow.Domain.ValueObjects;

namespace EngiFlow.Application.Abstractions.Tenancy;

/// <summary>
/// Supplies the tenant and actor identifiers that scope application use cases.
/// </summary>
/// <remarks>
/// Request-time implementations should derive these identifiers from trusted authentication
/// context so handlers do not accept spoofable actor identifiers from external request DTOs.
/// </remarks>
public interface ITenantProvider
{
    /// <summary>
    /// Gets the company tenant that should be applied to tenant-scoped operations.
    /// </summary>
    CompanyId CurrentCompanyId { get; }

    /// <summary>
    /// Gets the current user performing application commands.
    /// </summary>
    UserId CurrentUserId { get; }
}
