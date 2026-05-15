namespace EngiFlow.Application.Exceptions;

/// <summary>
/// Represents an application lookup failure for a tenant-scoped resource.
/// </summary>
public sealed class EntityNotFoundException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EntityNotFoundException"/> class.
    /// </summary>
    /// <param name="entityName">The entity type that could not be found.</param>
    /// <param name="identifier">The identifier used for the lookup.</param>
    public EntityNotFoundException(string entityName, Guid identifier)
        : base($"{entityName} '{identifier}' was not found.")
    {
        EntityName = entityName;
        Identifier = identifier;
    }

    /// <summary>
    /// Gets the entity type that could not be found.
    /// </summary>
    public string EntityName { get; }

    /// <summary>
    /// Gets the identifier used for the failed lookup.
    /// </summary>
    public Guid Identifier { get; }
}
