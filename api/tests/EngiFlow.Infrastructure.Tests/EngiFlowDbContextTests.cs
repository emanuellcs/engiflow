using EngiFlow.Application.Abstractions.Tenancy;
using EngiFlow.Domain.Companies;
using EngiFlow.Domain.Ecos;
using EngiFlow.Domain.Users;
using EngiFlow.Domain.ValueObjects;
using EngiFlow.Infrastructure.Persistence;
using EngiFlow.Infrastructure.Persistence.Interceptors;
using EngiFlow.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace EngiFlow.Infrastructure.Tests;

/// <summary>
/// Verifies the persistence behaviors that protect tenant boundaries and audit durability.
/// </summary>
/// <remarks>
/// These tests focus on model and unit-of-work behavior without requiring a live
/// PostgreSQL database. The generated EF migration remains the source of truth for the
/// actual relational schema.
/// </remarks>
public sealed class EngiFlowDbContextTests
{
    [Fact]
    public async Task TenantQueryFilter_ReturnsOnlyCurrentTenantRows()
    {
        var databaseName = Guid.NewGuid().ToString();
        var companyA = CompanyId.New();
        var companyB = CompanyId.New();

        await using (var context = CreateContext(databaseName, companyA))
        {
            context.Users.Add(User.Create(companyA, "requester-a@engiflow.example", "Requester A", UserRole.Requester));
            await context.SaveChangesAsync();
        }

        await using (var context = CreateContext(databaseName, companyB))
        {
            context.Users.Add(User.Create(companyB, "requester-b@engiflow.example", "Requester B", UserRole.Requester));
            await context.SaveChangesAsync();
        }

        await using (var context = CreateContext(databaseName, companyA))
        {
            var visibleUsers = await context.Users.ToListAsync();

            var visibleUser = Assert.Single(visibleUsers);
            Assert.Equal(companyA, visibleUser.CompanyId);
            Assert.Equal("requester-a@engiflow.example", visibleUser.Email);
        }
    }

    [Fact]
    public async Task SaveChanges_WhenTenantScopedEntityBelongsToDifferentTenant_Throws()
    {
        var currentCompanyId = CompanyId.New();
        var otherCompanyId = CompanyId.New();

        await using var context = CreateContext(Guid.NewGuid().ToString(), currentCompanyId);
        context.Users.Add(User.Create(otherCompanyId, "intruder@engiflow.example", "Intruder", UserRole.Requester));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());

        Assert.Contains("current tenant", exception.Message);
    }

    [Fact]
    public async Task SaveChanges_AllowsNewCompanyBootstrapGraphWithoutCurrentTenant()
    {
        var databaseName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<EngiFlowDbContext>()
            .UseInMemoryDatabase(databaseName)
            .AddInterceptors(new EcoAuditSaveChangesInterceptor())
            .Options;
        var company = Company.Create("Acme Engineering");
        var admin = company.RegisterUser(
            "admin@acme.example",
            "Administrator",
            UserRole.Administrator);
        admin.SetPasswordHash("hashed-password");

        await using var context = new EngiFlowDbContext(options, new ThrowingTenantProvider());
        context.Companies.Add(company);
        await context.SaveChangesAsync();

        Assert.Equal(1, await context.Companies.CountAsync());
        Assert.Equal(1, await context.Users.IgnoreQueryFilters().CountAsync());
    }

    [Fact]
    public async Task SaveChanges_WhenTenantScopedInsertIsNotNewCompanyGraph_RequiresCurrentTenant()
    {
        var options = new DbContextOptionsBuilder<EngiFlowDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(new EcoAuditSaveChangesInterceptor())
            .Options;

        await using var context = new EngiFlowDbContext(options, new ThrowingTenantProvider());
        context.Users.Add(User.Create(
            CompanyId.New(),
            "admin@acme.example",
            "Administrator",
            UserRole.Administrator));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task AuthenticationLookup_CanIgnoreTenantQueryFilter()
    {
        var databaseName = Guid.NewGuid().ToString();
        var companyA = CompanyId.New();
        var companyB = CompanyId.New();

        await using (var context = CreateContext(databaseName, companyB))
        {
            var user = User.Create(companyB, "admin@engiflow.local", "Administrator", UserRole.Administrator);
            user.SetPasswordHash("hashed-password");
            context.Users.Add(user);
            await context.SaveChangesAsync();
        }

        await using (var context = CreateContext(databaseName, companyA))
        {
            Assert.Empty(await context.Users.ToListAsync());

            var authenticatedUser = await context.Users
                .IgnoreQueryFilters()
                .SingleOrDefaultAsync(user => user.Email == "admin@engiflow.local");

            Assert.NotNull(authenticatedUser);
            Assert.Equal(companyB, authenticatedUser.CompanyId);
        }
    }

    [Fact]
    public void UserModel_StoresPasswordHashAndUsesGlobalEmailIndex()
    {
        using var context = CreateContext(Guid.NewGuid().ToString(), CompanyId.New());
        var userEntity = context.Model.FindEntityType(typeof(User))!;
        var passwordHash = userEntity.FindProperty(nameof(User.PasswordHash))!;

        Assert.Equal(typeof(string), passwordHash.ClrType);
        Assert.False(passwordHash.IsNullable);
        Assert.Equal(512, passwordHash.GetMaxLength());
        Assert.Contains(
            userEntity.GetIndexes(),
            index => index.GetDatabaseName() == "ux_users_email" && index.IsUnique);
    }

    [Fact]
    public async Task SaveChanges_PersistsPendingEcoEventsOnceAndClearsPendingBuffer()
    {
        var companyId = CompanyId.New();
        var actorUserId = UserId.New();
        var databaseName = Guid.NewGuid().ToString();

        await using var context = CreateContext(databaseName, companyId);
        var eco = EngineeringChangeOrder.Create(
            companyId,
            "Use aluminum bracket",
            "Update load-bearing bracket material from steel to aluminum.",
            EcoPriority.Medium,
            actorUserId);

        context.EngineeringChangeOrders.Add(eco);
        await context.SaveChangesAsync();
        await context.SaveChangesAsync();

        eco.SubmitForReview(actorUserId);
        await context.SaveChangesAsync();
        await context.SaveChangesAsync();

        Assert.Empty(eco.PendingEvents);
        Assert.Equal(2, await context.EcoEvents.CountAsync());
    }

    [Fact]
    public void Model_UsesConvertersForStrongIdsAndEnumStrings()
    {
        using var context = CreateContext(Guid.NewGuid().ToString(), CompanyId.New());

        var userEntity = context.Model.FindEntityType(typeof(User))!;
        var ecoEntity = context.Model.FindEntityType(typeof(EngineeringChangeOrder))!;
        var ecoEventEntity = context.Model.FindEntityType(typeof(EcoEvent))!;

        Assert.Equal(
            typeof(Guid),
            GetProviderClrType(userEntity.FindProperty(nameof(User.Id))!));
        Assert.Equal(
            typeof(Guid),
            GetProviderClrType(userEntity.FindProperty(nameof(User.CompanyId))!));
        Assert.Equal(
            typeof(string),
            GetProviderClrType(userEntity.FindProperty(nameof(User.Role))!));
        Assert.Equal(
            typeof(Guid),
            GetProviderClrType(ecoEntity.FindProperty(nameof(EngineeringChangeOrder.Id))!));
        Assert.Equal(
            typeof(string),
            GetProviderClrType(ecoEntity.FindProperty(nameof(EngineeringChangeOrder.Status))!));
        Assert.Equal(
            typeof(string),
            GetProviderClrType(ecoEntity.FindProperty(nameof(EngineeringChangeOrder.Priority))!));
        Assert.Equal(
            typeof(Guid),
            GetProviderClrType(ecoEventEntity.FindProperty(nameof(EcoEvent.Id))!));
        Assert.Equal(
            typeof(Guid),
            GetProviderClrType(ecoEventEntity.FindProperty(nameof(EcoEvent.EngineeringChangeOrderId))!));
        Assert.Equal(
            typeof(Guid),
            GetProviderClrType(ecoEventEntity.FindProperty(nameof(EcoEvent.ActorUserId))!));
        Assert.Equal(
            typeof(string),
            GetProviderClrType(ecoEventEntity.FindProperty(nameof(EcoEvent.EventType))!));
    }

    private static EngiFlowDbContext CreateContext(string databaseName, CompanyId companyId)
    {
        var options = new DbContextOptionsBuilder<EngiFlowDbContext>()
            .UseInMemoryDatabase(databaseName)
            .AddInterceptors(new EcoAuditSaveChangesInterceptor())
            .Options;

        return new EngiFlowDbContext(options, new StaticTenantProvider(companyId, UserId.New()));
    }

    private static Type? GetProviderClrType(IProperty property)
    {
        return property.GetValueConverter()?.ProviderClrType
            ?? property.GetTypeMapping().Converter?.ProviderClrType;
    }

    private sealed class ThrowingTenantProvider : ITenantProvider
    {
        public CompanyId CurrentCompanyId => throw new UnauthorizedAccessException("A valid tenant is required.");

        public UserId CurrentUserId => throw new UnauthorizedAccessException("A valid user is required.");
    }
}
