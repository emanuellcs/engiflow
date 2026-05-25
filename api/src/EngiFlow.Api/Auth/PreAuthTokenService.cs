using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EngiFlow.Application.Abstractions.Security;
using EngiFlow.Domain.ValueObjects;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace EngiFlow.Api.Auth;

/// <summary>
/// Issues and validates HMAC-signed pre-authentication tenant selection tokens.
/// </summary>
public sealed class PreAuthTokenService : IPreAuthTokenService
{
    private readonly JwtOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="PreAuthTokenService"/> class.
    /// </summary>
    /// <param name="options">The JWT signing options reused for pre-auth token signatures.</param>
    public PreAuthTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
        _options.Validate();
    }

    /// <inheritdoc />
    public PreAuthTokenResult CreatePreAuthToken(
        string normalizedEmail,
        IReadOnlyCollection<CompanyId> tenantIds,
        TimeSpan lifetime)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedEmail);
        if (tenantIds.Count == 0)
        {
            throw new ArgumentException("At least one tenant id is required.", nameof(tenantIds));
        }

        var expiresAt = DateTimeOffset.UtcNow.Add(lifetime);
        var payload = new PreAuthPayloadDto(
            normalizedEmail.Trim().ToLowerInvariant(),
            tenantIds.Select(tenantId => tenantId.Value).OrderBy(value => value).ToArray(),
            expiresAt);
        var payloadJson = JsonSerializer.Serialize(payload);
        var encodedPayload = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));
        var signature = Sign(encodedPayload);

        return new PreAuthTokenResult($"{encodedPayload}.{signature}", expiresAt);
    }

    /// <inheritdoc />
    public PreAuthTokenPayload ValidatePreAuthToken(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        var parts = token.Split('.', 2);

        if (parts.Length != 2 || !CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(Sign(parts[0])),
                Encoding.UTF8.GetBytes(parts[1])))
        {
            throw new UnauthorizedAccessException("The pre-authentication token is invalid.");
        }

        PreAuthPayloadDto payload;
        try
        {
            payload = JsonSerializer.Deserialize<PreAuthPayloadDto>(
                    Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(parts[0])))
                ?? throw new JsonException("Missing payload.");
        }
        catch (JsonException exception)
        {
            throw new UnauthorizedAccessException("The pre-authentication token is invalid.", exception);
        }

        if (payload.ExpiresAtUtc <= DateTimeOffset.UtcNow)
        {
            throw new UnauthorizedAccessException("The pre-authentication token has expired.");
        }

        return new PreAuthTokenPayload(
            payload.Email,
            payload.TenantIds.Select(CompanyId.From).ToArray(),
            payload.ExpiresAtUtc);
    }

    /// <summary>
    /// Signs a base64url encoded payload.
    /// </summary>
    /// <param name="encodedPayload">The encoded payload to sign.</param>
    /// <returns>The base64url encoded signature.</returns>
    private string Sign(string encodedPayload)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.SigningKey));
        return WebEncoders.Base64UrlEncode(hmac.ComputeHash(Encoding.UTF8.GetBytes(encodedPayload)));
    }

    private sealed record PreAuthPayloadDto(string Email, Guid[] TenantIds, DateTimeOffset ExpiresAtUtc);
}
