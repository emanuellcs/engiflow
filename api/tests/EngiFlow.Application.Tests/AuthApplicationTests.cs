using EngiFlow.Application.Abstractions.Persistence;
using EngiFlow.Application.Abstractions.Security;
using EngiFlow.Application.Abstractions.Tenancy;
using EngiFlow.Application.Auth.Commands;
using EngiFlow.Application.Auth.Dtos;
using EngiFlow.Application.Auth.Queries;
using EngiFlow.Application.Notifications.Commands;
using EngiFlow.Application.Notifications.Dtos;
using EngiFlow.Application.Notifications.Queries;
using EngiFlow.Domain.Companies;
using EngiFlow.Domain.Notifications;
using EngiFlow.Domain.Users;
using EngiFlow.Domain.ValueObjects;

namespace EngiFlow.Application.Tests;

public sealed class AuthApplicationTests
{
    [Fact]
    public async Task GetMyTenantsQueryHandler_ReturnsAvailableTenants()
    {
        // Arrange
        var company1 = Company.Create("Acme Corp", contactEmail: "contact@acme.com");
        var company2 = Company.Create("Globex", contactEmail: "hr@globex.com");
        
        var email = "user@example.com";
        var user1 = company1.RegisterUser(email, "John Doe", UserRole.Owner);
        var user2 = company2.RegisterUser(email, "John Doe", UserRole.Administrator);
        var owner2 = company2.RegisterUser("owner2@example.com", "Jane Smith", UserRole.Owner);
        
        var companies = new FakeCompanyRepository(company1, company2);
        var users = new FakeUserRepository(user1, user2, owner2);
        var tenantProvider = new FakeTenantProvider(company1.Id, user1.Id);
        
        var handler = new GetMyTenantsQueryHandler(companies, users, tenantProvider);

        // Act
        var result = await handler.HandleAsync(new GetMyTenantsQuery());

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, t => t.CompanyName == "Acme Corp" && t.OwnerName == "John Doe");
        Assert.Contains(result, t => t.CompanyName == "Globex" && t.OwnerName == "Jane Smith");
    }

    [Fact]
    public async Task SwitchTenantCommandHandler_WhenUserHasAccess_ReturnsNewToken()
    {
        // Arrange
        var company1 = Company.Create("Acme Corp");
        var company2 = Company.Create("Globex");
        
        var email = "user@example.com";
        var user1 = company1.RegisterUser(email, "John Doe", UserRole.Owner);
        var user2 = company2.RegisterUser(email, "John Doe", UserRole.Administrator);
        user2.SetPasswordHash("hash:secret");
        
        var companies = new FakeCompanyRepository(company1, company2);
        var users = new FakeUserRepository(user1, user2);
        var jwtTokenService = new FakeJwtTokenService();
        var tenantProvider = new FakeTenantProvider(company1.Id, user1.Id);
        
        var handler = new SwitchTenantCommandHandler(companies, users, jwtTokenService, tenantProvider);

        // Act
        var result = await handler.HandleAsync(new SwitchTenantCommand(company2.Id.Value));

        // Assert
        Assert.Equal("token:" + user2.Id.Value, result.AccessToken);
        Assert.Equal("Globex", result.CompanyName);
        Assert.Contains(nameof(UserRole.Administrator), result.Roles);
        Assert.Equal(user2.Id, users.LastSuccessfulLoginUserId);
    }

    [Fact]
    public async Task SwitchTenantCommandHandler_WhenUserInactive_ThrowsAuthenticationFailedException()
    {
        // Arrange
        var company1 = Company.Create("Acme Corp");
        var company2 = Company.Create("Globex");
        
        var email = "user@example.com";
        var user1 = company1.RegisterUser(email, "John Doe", UserRole.Owner);
        var user2 = company2.RegisterUser(email, "John Doe", UserRole.Administrator);
        user2.SetPasswordHash("hash:secret");
        user2.Deactivate(DateTimeOffset.UtcNow);
        
        var companies = new FakeCompanyRepository(company1, company2);
        var users = new FakeUserRepository(user1, user2);
        var jwtTokenService = new FakeJwtTokenService();
        var tenantProvider = new FakeTenantProvider(company1.Id, user1.Id);
        
        var handler = new SwitchTenantCommandHandler(companies, users, jwtTokenService, tenantProvider);

        // Act & Assert
        await Assert.ThrowsAsync<EngiFlow.Application.Exceptions.AuthenticationFailedException>(() => 
            handler.HandleAsync(new SwitchTenantCommand(company2.Id.Value)));
    }

    [Fact]
    public async Task SwitchTenantCommandHandler_WhenUserNotMember_ThrowsAuthenticationFailedException()
    {
        // Arrange
        var company1 = Company.Create("Acme Corp");
        var company2 = Company.Create("Globex");
        
        var user1 = company1.RegisterUser("user@example.com", "John Doe", UserRole.Owner);
        
        var companies = new FakeCompanyRepository(company1, company2);
        var users = new FakeUserRepository(user1);
        var jwtTokenService = new FakeJwtTokenService();
        var tenantProvider = new FakeTenantProvider(company1.Id, user1.Id);
        
        var handler = new SwitchTenantCommandHandler(companies, users, jwtTokenService, tenantProvider);

        // Act & Assert
        await Assert.ThrowsAsync<EngiFlow.Application.Exceptions.AuthenticationFailedException>(() => 
            handler.HandleAsync(new SwitchTenantCommand(company2.Id.Value)));
    }

    private sealed class FakeTenantProvider : ITenantProvider
    {
        public FakeTenantProvider(CompanyId currentCompanyId, UserId currentUserId)
        {
            CurrentCompanyId = currentCompanyId;
            CurrentUserId = currentUserId;
        }

        public CompanyId CurrentCompanyId { get; }

        public UserId CurrentUserId { get; }
    }

    private sealed class FakeCompanyRepository : ICompanyRepository
    {
        public FakeCompanyRepository(params Company[] companies)
        {
            Companies = companies.ToList();
        }

        public List<Company> Companies { get; } = [];

        public Task<Company?> GetByIdAsync(CompanyId id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Companies.SingleOrDefault(company => company.Id == id));
        }

        public Task<Company?> GetByIdForAuthenticationAsync(CompanyId id, CancellationToken cancellationToken = default)
        {
            return GetByIdAsync(id, cancellationToken);
        }

        public Task AddAsync(Company company, CancellationToken cancellationToken = default)
        {
            Companies.Add(company);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly IReadOnlyCollection<User> _users;

        public FakeUserRepository(params User[] users)
        {
            _users = users;
        }

        public UserId? LastSuccessfulLoginUserId { get; private set; }

        public DateTimeOffset? LastSuccessfulLoginAt { get; private set; }

        public Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_users.SingleOrDefault(user => user.Id == id));
        }

        public Task<IReadOnlyList<User>> ListByEmailForAuthenticationAsync(
            string normalizedEmail,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<User> users = _users
                .Where(user => user.Email == normalizedEmail)
                .ToArray();

            return Task.FromResult(users);
        }

        public Task<User?> GetByIdForAuthenticationAsync(UserId id, CancellationToken cancellationToken = default)
        {
            return GetByIdAsync(id, cancellationToken);
        }

        public Task RecordSuccessfulLoginAsync(
            UserId id,
            DateTimeOffset lastLoginAt,
            CancellationToken cancellationToken = default)
        {
            LastSuccessfulLoginUserId = id;
            LastSuccessfulLoginAt = lastLoginAt;
            return Task.CompletedTask;
        }

        public Task SetPasswordAndActivateAsync(
            UserId id,
            string passwordHash,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<User>> ListActiveAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<User>> ListForAdministrationAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<User>> SearchAsync(string term, int limit, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<User>>(
                _users.Where(u => u.DisplayName.Contains(term, StringComparison.OrdinalIgnoreCase) || 
                                u.Email.Contains(term, StringComparison.OrdinalIgnoreCase))
                    .Take(limit)
                    .ToList());
        }

        public Task<User?> GetOwnerByCompanyIdForAuthenticationAsync(
            CompanyId companyId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_users.SingleOrDefault(user =>
                user.CompanyId == companyId && user.Role == UserRole.Owner));
        }
    }

    private sealed class FakeJwtTokenService : IJwtTokenService
    {
        public DateTimeOffset ExpiresAtUtc { get; } = DateTimeOffset.Parse("2026-05-15T01:00:00Z");

        public AccessTokenResult CreateAccessToken(User user, string companyName)
        {
            return new AccessTokenResult($"token:{user.Id.Value}", ExpiresAtUtc);
        }
    }
}

public sealed class NotificationApplicationTests
{
    [Fact]
    public async Task GetMyNotificationsQueryHandler_ReturnsRecentNotifications()
    {
        // Arrange
        var companyId = CompanyId.New();
        var userId = UserId.New();
        var notifications = new List<Notification>();
        for (int i = 0; i < 25; i++)
        {
            notifications.Add(Notification.Create(userId, companyId, $"Title {i}", $"Msg {i}", NotificationCategory.Update));
        }

        var repository = new FakeNotificationRepository(notifications);
        var tenantProvider = new FakeTenantProvider(companyId, userId);
        var handler = new GetMyNotificationsQueryHandler(repository, tenantProvider);

        // Act
        var result = await handler.HandleAsync(new GetMyNotificationsQuery());

        // Assert
        Assert.Equal(20, result.Count); // Capped at 20
    }

    [Fact]
    public async Task MarkAllNotificationsAsReadCommandHandler_UpdatesUserNotifications()
    {
        // Arrange
        var companyId = CompanyId.New();
        var userId = UserId.New();
        var n1 = Notification.Create(userId, companyId, "T1", "M1", NotificationCategory.Update);
        var n2 = Notification.Create(userId, companyId, "T2", "M2", NotificationCategory.Update);
        
        var repository = new FakeNotificationRepository([n1, n2]);
        var tenantProvider = new FakeTenantProvider(companyId, userId);
        var handler = new MarkAllNotificationsAsReadCommandHandler(repository, tenantProvider);

        // Act
        await handler.HandleAsync(new MarkAllNotificationsAsReadCommand());

        // Assert
        Assert.True(n1.IsRead);
        Assert.True(n2.IsRead);
    }

    private sealed class FakeTenantProvider : ITenantProvider
    {
        public FakeTenantProvider(CompanyId currentCompanyId, UserId currentUserId)
        {
            CurrentCompanyId = currentCompanyId;
            CurrentUserId = currentUserId;
        }

        public CompanyId CurrentCompanyId { get; }
        public UserId CurrentUserId { get; }
    }

    private sealed class FakeNotificationRepository : INotificationRepository
    {
        private readonly List<Notification> _notifications;

        public FakeNotificationRepository(IEnumerable<Notification> notifications)
        {
            _notifications = notifications.ToList();
        }

        public Task AddAsync(Notification notification, CancellationToken cancellationToken = default)
        {
            _notifications.Add(notification);
            return Task.CompletedTask;
        }

        public Task<Notification?> GetByIdAsync(NotificationId id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_notifications.SingleOrDefault(n => n.Id == id));
        }

        public Task<IReadOnlyList<Notification>> ListRecentAsync(UserId userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Notification>>(
                _notifications.Where(n => n.UserId == userId).Take(20).ToList());
        }

        public Task<int> MarkAllAsReadAsync(UserId userId, CancellationToken cancellationToken = default)
        {
            var targets = _notifications.Where(n => n.UserId == userId && !n.IsRead).ToList();
            foreach (var n in targets) n.MarkAsRead();
            return Task.FromResult(targets.Count);
        }
    }
}
