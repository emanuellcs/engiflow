using EngiFlow.Domain.ValueObjects;

namespace EngiFlow.Domain.Abstractions;

/// <summary>
/// Identifies domain objects whose data must be isolated by a company tenant boundary.
/// </summary>
/// <remarks>
/// EngiFlow is a multi-tenant SaaS product, so operational records must always carry
/// the owning <see cref="CompanyId"/>. Infrastructure will use this contract to apply
/// global tenant filters and prevent accidental cross-company reads or writes.
/// </remarks>
public interface ITenantScoped
{
    /// <summary>
    /// Gets the company tenant that owns the entity.
    /// </summary>
    CompanyId CompanyId { get; }
}
