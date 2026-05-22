using EngiFlow.Application.Abstractions.Tenancy;
using EngiFlow.Domain.Companies;
using EngiFlow.Domain.Users;
using EngiFlow.Domain.ValueObjects;
using EngiFlow.Infrastructure.Persistence;
using EngiFlow.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EngiFlow.Infrastructure.Tests;

public sealed class UserRepositoryTests
{
    [Fact]
    public async Task SearchAsync_ReturnsMatchingUsersWithinTenant()
    {
        var databaseName = Guid.NewGuid().ToString();
        var companyA = CompanyId.New();
        var companyB = CompanyId.New();
        
        var userA1 = User.Create(companyA, "matching@a.com", "John Doe", UserRole.Requester);
        var userA2 = User.Create(companyA, "other@a.com", "Jane Smith", UserRole.Requester);
        var userB1 = User.Create(companyB, "matching@b.com", "John Doe", UserRole.Requester);

        await using (var context = CreateContext(databaseName, companyA))
        {
            context.Users.AddRange(userA1, userA2);
            await context.SaveChangesAsync();
        }

        await using (var context = CreateContext(databaseName, companyB))
        {
            context.Users.Add(userB1);
            await context.SaveChangesAsync();
        }

        await using (var context = CreateContext(databaseName, companyA))
        {
            var repository = new UserRepository(context);
            var results = await repository.SearchAsync("John", 10);

            var result = Assert.Single(results);
            Assert.Equal(userA1.Id, result.Id);
            Assert.Equal("John Doe", result.DisplayName);
        }
    }

    private static EngiFlowDbContext CreateContext(string databaseName, CompanyId currentCompanyId)
    {
        var options = new DbContextOptionsBuilder<EngiFlowDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        var tenantProvider = new FakeTenantProvider(currentCompanyId);
        return new EngiFlowDbContext(options, tenantProvider);
    }

    private sealed class FakeTenantProvider : ITenantProvider
    {
        public FakeTenantProvider(CompanyId currentCompanyId) => CurrentCompanyId = currentCompanyId;
        public CompanyId CurrentCompanyId { get; }
        public UserId CurrentUserId => UserId.New();
    }
}
