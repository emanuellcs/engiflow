using EngiFlow.Domain.Exceptions;
using EngiFlow.Domain.Guards;
using EngiFlow.Domain.Users;
using EngiFlow.Domain.ValueObjects;

namespace EngiFlow.Domain.Companies;

/// <summary>
/// Tenant aggregate that owns users and scopes all company operational data.
/// </summary>
/// <remarks>
/// In EngiFlow, the company is the tenant boundary. Every operational aggregate is
/// ultimately associated with a company so infrastructure can enforce strict data
/// isolation across customer organizations.
/// </remarks>
public sealed class Company
{
    private readonly List<User> _users = [];

    /// <summary>
    /// Initializes a new empty instance of the <see cref="Company"/> class for EF Core materialization.
    /// </summary>
    private Company()
    {
    }

    /// <summary>
    /// Initializes a new active company tenant.
    /// </summary>
    /// <param name="id">The tenant identifier.</param>
    /// <param name="name">The tenant display name.</param>
    /// <param name="createdAt">The UTC creation timestamp.</param>
    private Company(CompanyId id, string name, DateTimeOffset createdAt)
    {
        Id = id;
        Name = name;
        IsActive = true;
        CreatedAt = createdAt;
    }

    /// <summary>
    /// Gets the tenant identifier. This value is also the company boundary for tenant isolation.
    /// </summary>
    public CompanyId Id { get; private set; }

    /// <summary>
    /// Gets the company display name.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether the tenant can be changed or receive new users.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when the tenant was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when the tenant was deactivated, when applicable.
    /// </summary>
    public DateTimeOffset? DeactivatedAt { get; private set; }

    /// <summary>
    /// Gets the users currently tracked by this tenant aggregate.
    /// </summary>
    /// <remarks>
    /// The collection is exposed as read-only so callers must use domain methods that
    /// preserve company ownership and user invariants.
    /// </remarks>
    public IReadOnlyCollection<User> Users => _users.AsReadOnly();

    /// <summary>
    /// Creates an active company tenant.
    /// </summary>
    /// <param name="name">The company display name.</param>
    /// <param name="createdAt">Optional timestamp used for deterministic tests or imports.</param>
    /// <returns>A new active company tenant.</returns>
    public static Company Create(string name, DateTimeOffset? createdAt = null)
    {
        return new Company(
            CompanyId.New(),
            DomainGuard.Required(name, nameof(name), 200),
            DomainGuard.UtcTimestamp(createdAt));
    }

    /// <summary>
    /// Renames an active company tenant.
    /// </summary>
    /// <param name="name">The new company display name.</param>
    /// <exception cref="DomainException">Thrown when the company is inactive or the name is invalid.</exception>
    public void Rename(string name)
    {
        EnsureActive();
        Name = DomainGuard.Required(name, nameof(name), 200);
    }

    /// <summary>
    /// Registers a new active user inside this company tenant.
    /// </summary>
    /// <param name="email">The user's email address.</param>
    /// <param name="displayName">The user's display name.</param>
    /// <param name="role">The initial role assigned to the user.</param>
    /// <param name="createdAt">Optional timestamp used for deterministic tests or imports.</param>
    /// <returns>The created user with this company's tenant identifier.</returns>
    /// <exception cref="DomainException">Thrown when the company is inactive or user data is invalid.</exception>
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

    /// <summary>
    /// Deactivates the tenant and prevents future tenant mutations.
    /// </summary>
    /// <param name="deactivatedAt">Optional timestamp used for deterministic tests or imports.</param>
    /// <remarks>
    /// Deactivation is idempotent so repeated administrative commands do not corrupt
    /// lifecycle history.
    /// </remarks>
    public void Deactivate(DateTimeOffset? deactivatedAt = null)
    {
        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        DeactivatedAt = DomainGuard.UtcTimestamp(deactivatedAt);
    }

    /// <summary>
    /// Reactivates the tenant and clears its deactivation timestamp.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        DeactivatedAt = null;
    }

    /// <summary>
    /// Ensures the tenant is active before applying mutations.
    /// </summary>
    /// <exception cref="DomainException">Thrown when the company is inactive.</exception>
    private void EnsureActive()
    {
        if (!IsActive)
        {
            throw new DomainException("Inactive companies cannot be changed.");
        }
    }
}
