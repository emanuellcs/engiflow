using System.Security.Cryptography;
using System.Text;
using EngiFlow.Application.Abstractions.Security;
using Microsoft.AspNetCore.WebUtilities;

namespace EngiFlow.Infrastructure.Security;

/// <summary>
/// Generates random setup tokens and hashes them for persistence.
/// </summary>
internal sealed class PasswordSetupTokenService : IPasswordSetupTokenService
{
    /// <inheritdoc />
    public string GenerateToken()
    {
        return WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
    }

    /// <inheritdoc />
    public string HashToken(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token.Trim()));
        return WebEncoders.Base64UrlEncode(bytes);
    }
}
