using EngiFlow.Domain.ValueObjects;

namespace EngiFlow.Domain.Abstractions;

/// <summary>
/// Marks entities that are isolated by a company tenant boundary.
/// </summary>
public interface ITenantScoped
{
    CompanyId CompanyId { get; }
}
