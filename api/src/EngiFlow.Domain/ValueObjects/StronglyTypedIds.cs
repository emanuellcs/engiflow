using EngiFlow.Domain.Exceptions;

namespace EngiFlow.Domain.ValueObjects;

/// <summary>
/// Strongly typed identifier for a company tenant.
/// </summary>
/// <remarks>
/// Company identifiers are the root tenant key in EngiFlow. Using a dedicated type
/// prevents accidental assignment of unrelated identifiers to tenant-scoped entities.
/// </remarks>
public readonly record struct CompanyId
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompanyId"/> record.
    /// </summary>
    /// <param name="value">The non-empty GUID value backing the identifier.</param>
    /// <exception cref="DomainException">Thrown when <paramref name="value"/> is empty.</exception>
    public CompanyId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException("Company id cannot be empty.");
        }

        Value = value;
    }

    /// <summary>
    /// Gets the underlying GUID value used for persistence and integration boundaries.
    /// </summary>
    public Guid Value { get; }

    /// <summary>
    /// Creates a new unique company identifier.
    /// </summary>
    /// <returns>A non-empty company identifier.</returns>
    public static CompanyId New() => new(Guid.NewGuid());

    /// <summary>
    /// Rehydrates a company identifier from an existing GUID.
    /// </summary>
    /// <param name="value">The persisted GUID value.</param>
    /// <returns>A validated company identifier.</returns>
    public static CompanyId From(Guid value) => new(value);

    /// <summary>
    /// Returns the canonical string representation of the underlying GUID.
    /// </summary>
    /// <returns>The identifier as a string.</returns>
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Strongly typed identifier for a company-scoped user.
/// </summary>
/// <remarks>
/// A user identifier is distinct from a company identifier even though both are backed
/// by GUIDs. The type boundary makes authorization and audit code harder to misuse.
/// </remarks>
public readonly record struct UserId
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserId"/> record.
    /// </summary>
    /// <param name="value">The non-empty GUID value backing the identifier.</param>
    /// <exception cref="DomainException">Thrown when <paramref name="value"/> is empty.</exception>
    public UserId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException("User id cannot be empty.");
        }

        Value = value;
    }

    /// <summary>
    /// Gets the underlying GUID value used for persistence and integration boundaries.
    /// </summary>
    public Guid Value { get; }

    /// <summary>
    /// Creates a new unique user identifier.
    /// </summary>
    /// <returns>A non-empty user identifier.</returns>
    public static UserId New() => new(Guid.NewGuid());

    /// <summary>
    /// Rehydrates a user identifier from an existing GUID.
    /// </summary>
    /// <param name="value">The persisted GUID value.</param>
    /// <returns>A validated user identifier.</returns>
    public static UserId From(Guid value) => new(value);

    /// <summary>
    /// Returns the canonical string representation of the underlying GUID.
    /// </summary>
    /// <returns>The identifier as a string.</returns>
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Strongly typed identifier for an engineering change order aggregate.
/// </summary>
/// <remarks>
/// The explicit type keeps ECO references separate from audit event and user references
/// throughout the approval workflow.
/// </remarks>
public readonly record struct EngineeringChangeOrderId
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EngineeringChangeOrderId"/> record.
    /// </summary>
    /// <param name="value">The non-empty GUID value backing the identifier.</param>
    /// <exception cref="DomainException">Thrown when <paramref name="value"/> is empty.</exception>
    public EngineeringChangeOrderId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException("Engineering change order id cannot be empty.");
        }

        Value = value;
    }

    /// <summary>
    /// Gets the underlying GUID value used for persistence and integration boundaries.
    /// </summary>
    public Guid Value { get; }

    /// <summary>
    /// Creates a new unique ECO identifier.
    /// </summary>
    /// <returns>A non-empty ECO identifier.</returns>
    public static EngineeringChangeOrderId New() => new(Guid.NewGuid());

    /// <summary>
    /// Rehydrates an ECO identifier from an existing GUID.
    /// </summary>
    /// <param name="value">The persisted GUID value.</param>
    /// <returns>A validated ECO identifier.</returns>
    public static EngineeringChangeOrderId From(Guid value) => new(value);

    /// <summary>
    /// Returns the canonical string representation of the underlying GUID.
    /// </summary>
    /// <returns>The identifier as a string.</returns>
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Strongly typed identifier for an immutable ECO audit event.
/// </summary>
/// <remarks>
/// Audit records have their own identity so they can be referenced independently from
/// the ECO aggregate while still remaining tenant scoped.
/// </remarks>
public readonly record struct EcoEventId
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EcoEventId"/> record.
    /// </summary>
    /// <param name="value">The non-empty GUID value backing the identifier.</param>
    /// <exception cref="DomainException">Thrown when <paramref name="value"/> is empty.</exception>
    public EcoEventId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException("ECO event id cannot be empty.");
        }

        Value = value;
    }

    /// <summary>
    /// Gets the underlying GUID value used for persistence and integration boundaries.
    /// </summary>
    public Guid Value { get; }

    /// <summary>
    /// Creates a new unique ECO event identifier.
    /// </summary>
    /// <returns>A non-empty ECO event identifier.</returns>
    public static EcoEventId New() => new(Guid.NewGuid());

    /// <summary>
    /// Rehydrates an ECO event identifier from an existing GUID.
    /// </summary>
    /// <param name="value">The persisted GUID value.</param>
    /// <returns>A validated ECO event identifier.</returns>
    public static EcoEventId From(Guid value) => new(value);

    /// <summary>
    /// Returns the canonical string representation of the underlying GUID.
    /// </summary>
    /// <returns>The identifier as a string.</returns>
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Strongly typed identifier for an ECO timeline comment.
/// </summary>
public readonly record struct EcoCommentId
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EcoCommentId"/> record.
    /// </summary>
    /// <param name="value">The non-empty GUID value backing the identifier.</param>
    /// <exception cref="DomainException">Thrown when <paramref name="value"/> is empty.</exception>
    public EcoCommentId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException("ECO comment id cannot be empty.");
        }

        Value = value;
    }

    /// <summary>
    /// Gets the underlying GUID value used for persistence and integration boundaries.
    /// </summary>
    public Guid Value { get; }

    /// <summary>
    /// Creates a new unique ECO comment identifier.
    /// </summary>
    /// <returns>A non-empty ECO comment identifier.</returns>
    public static EcoCommentId New() => new(Guid.NewGuid());

    /// <summary>
    /// Rehydrates an ECO comment identifier from an existing GUID.
    /// </summary>
    /// <param name="value">The persisted GUID value.</param>
    /// <returns>A validated ECO comment identifier.</returns>
    public static EcoCommentId From(Guid value) => new(value);

    /// <summary>
    /// Returns the canonical string representation of the underlying GUID.
    /// </summary>
    /// <returns>The identifier as a string.</returns>
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Strongly typed identifier for an ECO affected item.
/// </summary>
public readonly record struct EcoAffectedItemId
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EcoAffectedItemId"/> record.
    /// </summary>
    /// <param name="value">The non-empty GUID value backing the identifier.</param>
    /// <exception cref="DomainException">Thrown when <paramref name="value"/> is empty.</exception>
    public EcoAffectedItemId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException("ECO affected item id cannot be empty.");
        }

        Value = value;
    }

    /// <summary>
    /// Gets the underlying GUID value used for persistence and integration boundaries.
    /// </summary>
    public Guid Value { get; }

    /// <summary>
    /// Creates a new unique ECO affected item identifier.
    /// </summary>
    /// <returns>A non-empty ECO affected item identifier.</returns>
    public static EcoAffectedItemId New() => new(Guid.NewGuid());

    /// <summary>
    /// Rehydrates an ECO affected item identifier from an existing GUID.
    /// </summary>
    /// <param name="value">The persisted GUID value.</param>
    /// <returns>A validated ECO affected item identifier.</returns>
    public static EcoAffectedItemId From(Guid value) => new(value);

    /// <summary>
    /// Returns the canonical string representation of the underlying GUID.
    /// </summary>
    /// <returns>The identifier as a string.</returns>
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Strongly typed identifier for an ECO approval decision.
/// </summary>
public readonly record struct EcoApprovalId
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EcoApprovalId"/> record.
    /// </summary>
    /// <param name="value">The non-empty GUID value backing the identifier.</param>
    /// <exception cref="DomainException">Thrown when <paramref name="value"/> is empty.</exception>
    public EcoApprovalId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException("ECO approval id cannot be empty.");
        }

        Value = value;
    }

    /// <summary>
    /// Gets the underlying GUID value used for persistence and integration boundaries.
    /// </summary>
    public Guid Value { get; }

    /// <summary>
    /// Creates a new unique ECO approval identifier.
    /// </summary>
    /// <returns>A non-empty ECO approval identifier.</returns>
    public static EcoApprovalId New() => new(Guid.NewGuid());

    /// <summary>
    /// Rehydrates an ECO approval identifier from an existing GUID.
    /// </summary>
    /// <param name="value">The persisted GUID value.</param>
    /// <returns>A validated ECO approval identifier.</returns>
    public static EcoApprovalId From(Guid value) => new(value);

    /// <summary>
    /// Returns the canonical string representation of the underlying GUID.
    /// </summary>
    /// <returns>The identifier as a string.</returns>
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Strongly typed identifier for an ECO attachment metadata record.
/// </summary>
public readonly record struct EcoAttachmentId
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EcoAttachmentId"/> record.
    /// </summary>
    /// <param name="value">The non-empty GUID value backing the identifier.</param>
    /// <exception cref="DomainException">Thrown when <paramref name="value"/> is empty.</exception>
    public EcoAttachmentId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException("ECO attachment id cannot be empty.");
        }

        Value = value;
    }

    /// <summary>
    /// Gets the underlying GUID value used for persistence and integration boundaries.
    /// </summary>
    public Guid Value { get; }

    /// <summary>
    /// Creates a new unique ECO attachment identifier.
    /// </summary>
    /// <returns>A non-empty ECO attachment identifier.</returns>
    public static EcoAttachmentId New() => new(Guid.NewGuid());

    /// <summary>
    /// Rehydrates an ECO attachment identifier from an existing GUID.
    /// </summary>
    /// <param name="value">The persisted GUID value.</param>
    /// <returns>A validated ECO attachment identifier.</returns>
    public static EcoAttachmentId From(Guid value) => new(value);

    /// <summary>
    /// Returns the canonical string representation of the underlying GUID.
    /// </summary>
    /// <returns>The identifier as a string.</returns>
    public override string ToString() => Value.ToString();
}
