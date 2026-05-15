namespace EngiFlow.Application.Exceptions;

/// <summary>
/// Represents a failed authentication attempt without revealing which credential was invalid.
/// </summary>
public sealed class AuthenticationFailedException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticationFailedException"/> class.
    /// </summary>
    public AuthenticationFailedException()
        : base("Invalid email or password.")
    {
    }
}
