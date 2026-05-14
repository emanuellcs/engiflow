using EngiFlow.Domain.Exceptions;

namespace EngiFlow.Domain.Guards;

/// <summary>
/// Centralizes low-level invariant checks used by domain entities and value objects.
/// </summary>
/// <remarks>
/// Keeping guard clauses in the domain layer makes invariants consistent while avoiding
/// any dependency on validation libraries from the application or API layers.
/// </remarks>
internal static class DomainGuard
{
    /// <summary>
    /// Normalizes and validates required text.
    /// </summary>
    /// <param name="value">The candidate text.</param>
    /// <param name="parameterName">The business parameter name used in exception messages.</param>
    /// <param name="maxLength">The maximum accepted normalized length.</param>
    /// <returns>The trimmed text when it satisfies the invariant.</returns>
    /// <exception cref="DomainException">Thrown when the text is missing or too long.</exception>
    public static string Required(string? value, string parameterName, int maxLength)
    {
        var normalized = value?.Trim();

        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new DomainException($"{parameterName} is required.");
        }

        if (normalized.Length > maxLength)
        {
            throw new DomainException($"{parameterName} cannot exceed {maxLength} characters.");
        }

        return normalized;
    }

    /// <summary>
    /// Rejects default values for strongly typed identifiers and other value types.
    /// </summary>
    /// <typeparam name="TValue">The struct type being validated.</typeparam>
    /// <param name="value">The value to inspect.</param>
    /// <param name="parameterName">The business parameter name used in exception messages.</param>
    /// <exception cref="DomainException">Thrown when <paramref name="value"/> is the default value.</exception>
    public static void AgainstDefault<TValue>(TValue value, string parameterName)
        where TValue : struct
    {
        if (EqualityComparer<TValue>.Default.Equals(value, default))
        {
            throw new DomainException($"{parameterName} is required.");
        }
    }

    /// <summary>
    /// Ensures enum values are declared members rather than arbitrary integers.
    /// </summary>
    /// <typeparam name="TEnum">The enum type being validated.</typeparam>
    /// <param name="value">The enum value to inspect.</param>
    /// <param name="parameterName">The business parameter name used in exception messages.</param>
    /// <exception cref="DomainException">Thrown when the enum value is not defined.</exception>
    public static void AgainstInvalidEnum<TEnum>(TEnum value, string parameterName)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new DomainException($"{parameterName} is invalid.");
        }
    }

    /// <summary>
    /// Produces a UTC timestamp for durable audit and lifecycle records.
    /// </summary>
    /// <param name="value">An optional timestamp supplied by the caller, usually in tests or imports.</param>
    /// <returns>The supplied timestamp converted to UTC, or the current UTC time.</returns>
    public static DateTimeOffset UtcTimestamp(DateTimeOffset? value = null)
    {
        return (value ?? DateTimeOffset.UtcNow).ToUniversalTime();
    }
}
