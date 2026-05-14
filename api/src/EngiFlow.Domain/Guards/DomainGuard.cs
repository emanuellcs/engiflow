using EngiFlow.Domain.Exceptions;

namespace EngiFlow.Domain.Guards;

internal static class DomainGuard
{
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

    public static void AgainstDefault<TValue>(TValue value, string parameterName)
        where TValue : struct
    {
        if (EqualityComparer<TValue>.Default.Equals(value, default))
        {
            throw new DomainException($"{parameterName} is required.");
        }
    }

    public static void AgainstInvalidEnum<TEnum>(TEnum value, string parameterName)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new DomainException($"{parameterName} is invalid.");
        }
    }

    public static DateTimeOffset UtcTimestamp(DateTimeOffset? value = null)
    {
        return (value ?? DateTimeOffset.UtcNow).ToUniversalTime();
    }
}
