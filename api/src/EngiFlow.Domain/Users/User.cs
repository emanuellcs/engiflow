using System.Net.Mail;
using EngiFlow.Domain.Abstractions;
using EngiFlow.Domain.Exceptions;
using EngiFlow.Domain.Guards;
using EngiFlow.Domain.ValueObjects;

namespace EngiFlow.Domain.Users;

/// <summary>
/// Company-scoped identity with role-based authorization metadata.
/// </summary>
/// <remarks>
/// Users carry their company tenant identifier because approvals and audit entries must
/// always be attributable to an actor inside the same tenant boundary as the ECO.
/// </remarks>
public sealed class User : ITenantScoped
{
    /// <summary>
    /// Initializes a new empty instance of the <see cref="User"/> class for EF Core materialization.
    /// </summary>
    private User()
    {
    }

    /// <summary>
    /// Initializes a new company-scoped user.
    /// </summary>
    /// <param name="id">The user identifier.</param>
    /// <param name="companyId">The tenant identifier that owns the user.</param>
    /// <param name="email">The normalized email address.</param>
    /// <param name="displayName">The user display name.</param>
    /// <param name="role">The user's role in the ECO workflow.</param>
    /// <param name="status">The initial lifecycle status.</param>
    /// <param name="createdAt">The UTC creation timestamp.</param>
    private User(
        UserId id,
        CompanyId companyId,
        string email,
        string displayName,
        UserRole role,
        UserStatus status,
        DateTimeOffset createdAt)
    {
        Id = id;
        CompanyId = companyId;
        Email = email;
        DisplayName = displayName;
        Role = role;
        Status = status;
        CreatedAt = createdAt;
    }

    /// <summary>
    /// Gets the unique user identifier.
    /// </summary>
    public UserId Id { get; private set; }

    /// <summary>
    /// Gets the company tenant that owns the user.
    /// </summary>
    public CompanyId CompanyId { get; private set; }

    /// <summary>
    /// Gets the normalized email address used for identity and notifications.
    /// </summary>
    public string Email { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the display name shown in review and audit experiences.
    /// </summary>
    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the user's role in the ECO workflow.
    /// </summary>
    public UserRole Role { get; private set; }

    /// <summary>
    /// Gets the password hash used by the application security layer to verify credentials.
    /// </summary>
    /// <remarks>
    /// The domain stores only the opaque hash. Password hashing policy and token creation
    /// remain outside the domain boundary.
    /// </remarks>
    public string? PasswordHash { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the user can perform domain actions.
    /// </summary>
    public bool IsActive => Status == UserStatus.Active;

    /// <summary>
    /// Gets the lifecycle status of the user account.
    /// </summary>
    public UserStatus Status { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when the user was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when the user last authenticated successfully.
    /// </summary>
    public DateTimeOffset? LastLoginAt { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when the user was deactivated, when applicable.
    /// </summary>
    public DateTimeOffset? DeactivatedAt { get; private set; }

    /// <summary>
    /// Creates an active user inside a company tenant.
    /// </summary>
    /// <param name="companyId">The tenant identifier that owns the user.</param>
    /// <param name="email">The user's email address.</param>
    /// <param name="displayName">The user's display name.</param>
    /// <param name="role">The user's initial workflow role.</param>
    /// <param name="createdAt">Optional timestamp used for deterministic tests or imports.</param>
    /// <returns>A new active user.</returns>
    /// <exception cref="DomainException">Thrown when tenant, email, display name, or role data is invalid.</exception>
    public static User Create(
        CompanyId companyId,
        string email,
        string displayName,
        UserRole role,
        DateTimeOffset? createdAt = null)
    {
        DomainGuard.AgainstDefault(companyId, nameof(companyId));
        DomainGuard.AgainstInvalidEnum(role, nameof(role));

        return new User(
            UserId.New(),
            companyId,
            NormalizeEmail(email),
            DomainGuard.Required(displayName, nameof(displayName), 200),
            role,
            UserStatus.Active,
            DomainGuard.UtcTimestamp(createdAt));
    }

    /// <summary>
    /// Creates a pending invitation user inside a company tenant.
    /// </summary>
    /// <param name="companyId">The tenant identifier that owns the user.</param>
    /// <param name="email">The user's email address.</param>
    /// <param name="displayName">The user's display name.</param>
    /// <param name="role">The user's initial workflow role.</param>
    /// <param name="createdAt">Optional timestamp used for deterministic tests or imports.</param>
    /// <returns>A new pending activation user.</returns>
    /// <exception cref="DomainException">Thrown when tenant, email, display name, or role data is invalid.</exception>
    public static User CreatePendingActivation(
        CompanyId companyId,
        string email,
        string displayName,
        UserRole role,
        DateTimeOffset? createdAt = null)
    {
        DomainGuard.AgainstDefault(companyId, nameof(companyId));
        DomainGuard.AgainstInvalidEnum(role, nameof(role));

        if (role == UserRole.Owner)
        {
            throw new DomainException("Owner users cannot be created through invitations.");
        }

        return new User(
            UserId.New(),
            companyId,
            NormalizeEmail(email),
            DomainGuard.Required(displayName, nameof(displayName), 200),
            role,
            UserStatus.PendingActivation,
            DomainGuard.UtcTimestamp(createdAt));
    }

    /// <summary>
    /// Changes the display name of an active user.
    /// </summary>
    /// <param name="displayName">The new display name.</param>
    /// <exception cref="DomainException">Thrown when the user is inactive or the display name is invalid.</exception>
    public void Rename(string displayName)
    {
        EnsureMutable();
        DisplayName = DomainGuard.Required(displayName, nameof(displayName), 200);
    }

    /// <summary>
    /// Changes the user's role while preserving the tenant identity.
    /// </summary>
    /// <param name="role">The new role.</param>
    /// <exception cref="DomainException">Thrown when the user is inactive or the role is invalid.</exception>
    public void ChangeRole(UserRole role)
    {
        EnsureMutable();
        DomainGuard.AgainstInvalidEnum(role, nameof(role));

        if (Role == UserRole.Owner || role == UserRole.Owner)
        {
            throw new DomainException("The Owner role is immutable.");
        }

        Role = role;
    }

    /// <summary>
    /// Stores a credential hash generated by the application security layer.
    /// </summary>
    /// <param name="passwordHash">The opaque, non-empty password hash.</param>
    /// <exception cref="DomainException">Thrown when the hash is missing or exceeds the storage limit.</exception>
    public void SetPasswordHash(string passwordHash)
    {
        PasswordHash = DomainGuard.Required(passwordHash, nameof(passwordHash), 512);
    }

    /// <summary>
    /// Clears the stored credential hash so the user cannot authenticate.
    /// </summary>
    public void ClearPasswordHash()
    {
        PasswordHash = null;
    }

    /// <summary>
    /// Records a successful authentication timestamp.
    /// </summary>
    /// <param name="loggedInAt">Optional timestamp used for deterministic tests.</param>
    public void RecordSuccessfulLogin(DateTimeOffset? loggedInAt = null)
    {
        EnsureActive();
        LastLoginAt = DomainGuard.UtcTimestamp(loggedInAt);
    }

    /// <summary>
    /// Activates a pending invitation after credentials are established.
    /// </summary>
    /// <param name="passwordHash">The opaque credential hash generated by the security layer.</param>
    /// <exception cref="DomainException">Thrown when the account is deactivated.</exception>
    public void ActivateWithPassword(string passwordHash)
    {
        if (Status == UserStatus.Deactivated)
        {
            throw new DomainException("Deactivated users cannot be activated through invitation setup.");
        }

        SetPasswordHash(passwordHash);
        Status = UserStatus.Active;
        DeactivatedAt = null;
    }

    /// <summary>
    /// Deactivates the user and prevents the user from acting in the domain.
    /// </summary>
    /// <param name="deactivatedAt">Optional timestamp used for deterministic tests or imports.</param>
    /// <remarks>
    /// Deactivation is idempotent so repeated administrative commands do not rewrite
    /// lifecycle history.
    /// </remarks>
    public void Deactivate(DateTimeOffset? deactivatedAt = null)
    {
        if (Role == UserRole.Owner)
        {
            throw new DomainException("Owner users cannot be deactivated.");
        }

        if (Status == UserStatus.Deactivated)
        {
            return;
        }

        Status = UserStatus.Deactivated;
        DeactivatedAt = DomainGuard.UtcTimestamp(deactivatedAt);
    }

    /// <summary>
    /// Reactivates the user and clears the deactivation timestamp.
    /// </summary>
    public void Activate()
    {
        Status = UserStatus.Active;
        DeactivatedAt = null;
    }

    /// <summary>
    /// Ensures the user is active before performing a business action.
    /// </summary>
    /// <exception cref="DomainException">Thrown when the user is inactive.</exception>
    public void EnsureActive()
    {
        if (!IsActive)
        {
            throw new DomainException("Inactive users cannot perform this action.");
        }
    }

    /// <summary>
    /// Ensures the user can be administratively mutated.
    /// </summary>
    /// <exception cref="DomainException">Thrown when the user is deactivated.</exception>
    private void EnsureMutable()
    {
        if (Status == UserStatus.Deactivated)
        {
            throw new DomainException("Deactivated users cannot be changed.");
        }
    }

    /// <summary>
    /// Normalizes and validates an email address.
    /// </summary>
    /// <param name="email">The candidate email address.</param>
    /// <returns>The normalized email address.</returns>
    /// <exception cref="DomainException">Thrown when the email is missing or malformed.</exception>
    private static string NormalizeEmail(string email)
    {
        var normalized = DomainGuard.Required(email, nameof(email), 320).ToLowerInvariant();

        try
        {
            var address = new MailAddress(normalized);
            if (!string.Equals(address.Address, normalized, StringComparison.OrdinalIgnoreCase))
            {
                throw new DomainException("Email is invalid.");
            }
        }
        catch (FormatException exception)
        {
            throw new DomainException("Email is invalid.", exception);
        }

        return normalized;
    }
}
