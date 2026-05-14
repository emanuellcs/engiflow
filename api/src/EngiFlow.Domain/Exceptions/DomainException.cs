namespace EngiFlow.Domain.Exceptions;

/// <summary>
/// Represents a business-rule violation raised by the domain model.
/// </summary>
/// <remarks>
/// Domain exceptions are intentionally independent from transport, persistence, and
/// validation frameworks. Application use cases can translate them into API responses
/// without coupling the domain layer to ASP.NET Core.
/// </remarks>
public sealed class DomainException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DomainException"/> class.
    /// </summary>
    /// <param name="message">Human-readable description of the violated business rule.</param>
    public DomainException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainException"/> class with an inner exception.
    /// </summary>
    /// <param name="message">Human-readable description of the violated business rule.</param>
    /// <param name="innerException">The lower-level exception that caused the domain failure.</param>
    public DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
