using System.Net.Mail;
using EngiFlow.Domain.Abstractions;
using EngiFlow.Domain.Exceptions;
using EngiFlow.Domain.Guards;
using EngiFlow.Domain.ValueObjects;

namespace EngiFlow.Domain.Users;

/// <summary>
/// Company-scoped identity with role-based authorization metadata.
/// </summary>
public sealed class User : ITenantScoped
{
    private User()
    {
    }

    private User(
        UserId id,
        CompanyId companyId,
        string email,
        string displayName,
        UserRole role,
        DateTimeOffset createdAt)
    {
        Id = id;
        CompanyId = companyId;
        Email = email;
        DisplayName = displayName;
        Role = role;
        IsActive = true;
        CreatedAt = createdAt;
    }

    public UserId Id { get; private set; }

    public CompanyId CompanyId { get; private set; }

    public string Email { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public UserRole Role { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? DeactivatedAt { get; private set; }

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
            DomainGuard.UtcTimestamp(createdAt));
    }

    public void Rename(string displayName)
    {
        EnsureActive();
        DisplayName = DomainGuard.Required(displayName, nameof(displayName), 200);
    }

    public void ChangeRole(UserRole role)
    {
        EnsureActive();
        DomainGuard.AgainstInvalidEnum(role, nameof(role));
        Role = role;
    }

    public void Deactivate(DateTimeOffset? deactivatedAt = null)
    {
        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        DeactivatedAt = DomainGuard.UtcTimestamp(deactivatedAt);
    }

    public void Activate()
    {
        IsActive = true;
        DeactivatedAt = null;
    }

    public void EnsureActive()
    {
        if (!IsActive)
        {
            throw new DomainException("Inactive users cannot perform this action.");
        }
    }

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
