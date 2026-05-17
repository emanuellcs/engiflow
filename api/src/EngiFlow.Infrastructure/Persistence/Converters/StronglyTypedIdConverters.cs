using EngiFlow.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace EngiFlow.Infrastructure.Persistence.Converters;

/// <summary>
/// Centralizes EF Core value converters for strongly typed domain identifiers.
/// </summary>
/// <remarks>
/// The domain intentionally wraps GUIDs in distinct value objects to prevent accidental
/// cross-assignment of identifiers. EF Core still stores them as PostgreSQL UUID values,
/// so converters provide the persistence boundary without leaking database concerns into
/// the domain model.
/// </remarks>
internal static class StronglyTypedIdConverters
{
    /// <summary>
    /// Converts <see cref="CompanyId"/> values to and from database UUID values.
    /// </summary>
    public static readonly ValueConverter<CompanyId, Guid> CompanyId =
        new(id => id.Value, value => Domain.ValueObjects.CompanyId.From(value));

    /// <summary>
    /// Converts <see cref="UserId"/> values to and from database UUID values.
    /// </summary>
    public static readonly ValueConverter<UserId, Guid> UserId =
        new(id => id.Value, value => Domain.ValueObjects.UserId.From(value));

    /// <summary>
    /// Converts <see cref="EngineeringChangeOrderId"/> values to and from database UUID values.
    /// </summary>
    public static readonly ValueConverter<EngineeringChangeOrderId, Guid> EngineeringChangeOrderId =
        new(id => id.Value, value => Domain.ValueObjects.EngineeringChangeOrderId.From(value));

    /// <summary>
    /// Converts <see cref="EcoEventId"/> values to and from database UUID values.
    /// </summary>
    public static readonly ValueConverter<EcoEventId, Guid> EcoEventId =
        new(id => id.Value, value => Domain.ValueObjects.EcoEventId.From(value));

    /// <summary>
    /// Converts <see cref="EcoCommentId"/> values to and from database UUID values.
    /// </summary>
    public static readonly ValueConverter<EcoCommentId, Guid> EcoCommentId =
        new(id => id.Value, value => Domain.ValueObjects.EcoCommentId.From(value));

    /// <summary>
    /// Converts <see cref="EcoAffectedItemId"/> values to and from database UUID values.
    /// </summary>
    public static readonly ValueConverter<EcoAffectedItemId, Guid> EcoAffectedItemId =
        new(id => id.Value, value => Domain.ValueObjects.EcoAffectedItemId.From(value));

    /// <summary>
    /// Converts <see cref="EcoApprovalId"/> values to and from database UUID values.
    /// </summary>
    public static readonly ValueConverter<EcoApprovalId, Guid> EcoApprovalId =
        new(id => id.Value, value => Domain.ValueObjects.EcoApprovalId.From(value));

    /// <summary>
    /// Converts <see cref="EcoAttachmentId"/> values to and from database UUID values.
    /// </summary>
    public static readonly ValueConverter<EcoAttachmentId, Guid> EcoAttachmentId =
        new(id => id.Value, value => Domain.ValueObjects.EcoAttachmentId.From(value));
}
