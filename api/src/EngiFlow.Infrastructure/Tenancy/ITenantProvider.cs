using EngiFlow.Domain.ValueObjects;

namespace EngiFlow.Infrastructure.Tenancy;

/// <summary>
/// Supplies the tenant identifier that scopes infrastructure reads and writes.
/// </summary>
/// <remarks>
/// The current implementation is intentionally small because authentication is not
/// part of Step 3. Keeping this contract explicit lets the API later replace the
/// static development tenant with a JWT-backed provider without changing DbContext
/// mappings or repository code.
/// </remarks>
public interface ITenantProvider
{
    /// <summary>
    /// Gets the company tenant that should be applied to tenant-scoped operations.
    /// </summary>
    CompanyId CurrentCompanyId { get; }
}
