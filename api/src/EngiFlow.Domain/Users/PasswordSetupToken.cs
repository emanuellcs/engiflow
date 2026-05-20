using EngiFlow.Domain.Guards;
using EngiFlow.Domain.ValueObjects;

namespace EngiFlow.Domain.Users;

/// <summary>
/// Stores a one-time hashed password setup or reset token for a user account.
/// </summary>
public sealed class PasswordSetupToken
{
    /// <summary>
    /// Initializes a new empty instance of the <see cref="PasswordSetupToken"/> class for EF Core materialization.
    /// </summary>
    private PasswordSetupToken()
    {
    }

    private PasswordSetupToken(
        PasswordSetupTokenId id,
        UserId userId,
        PasswordSetupTokenPurpose purpose,
        string tokenHash,
        DateTimeOffset expiresAt,
        DateTimeOffset createdAt)
    {
        Id = id;
        UserId = userId;
        Purpose = purpose;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
        CreatedAt = createdAt;
    }

    /// <summary>
    /// Gets the unique token record identifier.
    /// </summary>
    public PasswordSetupTokenId Id { get; private set; }

    /// <summary>
    /// Gets the user that owns the token.
    /// </summary>
    public UserId UserId { get; private set; }

    /// <summary>
    /// Gets the purpose represented by the token.
    /// </summary>
    public PasswordSetupTokenPurpose Purpose { get; private set; }

    /// <summary>
    /// Gets the non-reversible token hash.
    /// </summary>
    public string TokenHash { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the UTC timestamp when the token was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp after which the token is invalid.
    /// </summary>
    public DateTimeOffset ExpiresAt { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when the token was consumed, when applicable.
    /// </summary>
    public DateTimeOffset? ConsumedAt { get; private set; }

    /// <summary>
    /// Creates a new unconsumed password token record.
    /// </summary>
    /// <param name="userId">The user that owns the token.</param>
    /// <param name="purpose">The token purpose.</param>
    /// <param name="tokenHash">The hashed token value.</param>
    /// <param name="expiresAt">The UTC expiration timestamp.</param>
    /// <param name="createdAt">Optional timestamp used for deterministic tests.</param>
    /// <returns>A validated token record.</returns>
    public static PasswordSetupToken Create(
        UserId userId,
        PasswordSetupTokenPurpose purpose,
        string tokenHash,
        DateTimeOffset expiresAt,
        DateTimeOffset? createdAt = null)
    {
        DomainGuard.AgainstDefault(userId, nameof(userId));
        DomainGuard.AgainstInvalidEnum(purpose, nameof(purpose));

        return new PasswordSetupToken(
            PasswordSetupTokenId.New(),
            userId,
            purpose,
            DomainGuard.Required(tokenHash, nameof(tokenHash), 128),
            DomainGuard.UtcTimestamp(expiresAt),
            DomainGuard.UtcTimestamp(createdAt));
    }

    /// <summary>
    /// Gets a value indicating whether the token can be consumed at the supplied time.
    /// </summary>
    /// <param name="utcNow">The UTC timestamp to test against.</param>
    /// <returns><see langword="true"/> when the token is unconsumed and not expired.</returns>
    public bool IsUsable(DateTimeOffset utcNow)
    {
        return ConsumedAt is null && DomainGuard.UtcTimestamp(utcNow) <= ExpiresAt;
    }

    /// <summary>
    /// Marks the token as consumed.
    /// </summary>
    /// <param name="consumedAt">Optional timestamp used for deterministic tests.</param>
    public void Consume(DateTimeOffset? consumedAt = null)
    {
        if (ConsumedAt is not null)
        {
            return;
        }

        ConsumedAt = DomainGuard.UtcTimestamp(consumedAt);
    }
}
