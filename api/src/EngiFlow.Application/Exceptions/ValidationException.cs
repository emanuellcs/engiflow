namespace EngiFlow.Application.Exceptions;

/// <summary>
/// Represents one or more application request validation failures.
/// </summary>
public sealed class ValidationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationException"/> class.
    /// </summary>
    /// <param name="errors">Validation errors grouped by request property name.</param>
    public ValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base("One or more validation failures occurred.")
    {
        Errors = errors;
    }

    /// <summary>
    /// Gets validation errors grouped by request property name.
    /// </summary>
    public IReadOnlyDictionary<string, string[]> Errors { get; }
}
