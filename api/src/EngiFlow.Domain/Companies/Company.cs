using EngiFlow.Domain.Exceptions;
using EngiFlow.Domain.Guards;
using EngiFlow.Domain.Users;
using EngiFlow.Domain.ValueObjects;

namespace EngiFlow.Domain.Companies;

/// <summary>
/// Tenant aggregate that owns users and scopes all operational data.
/// </summary>
public sealed class Company
{
    private readonly List<User> _users = [];

    private Company()
    {
    }

    private Company(CompanyId id, string name, DateTimeOffset createdAt)
    {
        Id = id;
        Name = name;
        IsActive = true;
        CreatedAt = createdAt;
    }

    public CompanyId Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? DeactivatedAt { get; private set; }

    public IReadOnlyCollection<User> Users => _users.AsReadOnly();

    public static Company Create(string name, DateTimeOffset? createdAt = null)
    {
        return new Company(
            CompanyId.New(),
            DomainGuard.Required(name, nameof(name), 200),
            DomainGuard.UtcTimestamp(createdAt));
    }

    public void Rename(string name)
    {
        EnsureActive();
        Name = DomainGuard.Required(name, nameof(name), 200);
    }

    public User RegisterUser(
        string email,
        string displayName,
        UserRole role,
        DateTimeOffset? createdAt = null)
    {
        EnsureActive();

        var user = User.Create(Id, email, displayName, role, createdAt);
        _users.Add(user);
        return user;
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

    private void EnsureActive()
    {
        if (!IsActive)
        {
            throw new DomainException("Inactive companies cannot be changed.");
        }
    }
}
