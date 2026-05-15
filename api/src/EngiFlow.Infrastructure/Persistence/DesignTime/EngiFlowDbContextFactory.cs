using EngiFlow.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EngiFlow.Infrastructure.Persistence.DesignTime;

/// <summary>
/// Creates <see cref="EngiFlowDbContext"/> instances for EF Core design-time tooling.
/// </summary>
/// <remarks>
/// Migrations must be generated without relying on the web host to boot. The factory
/// uses environment variables when available and otherwise falls back to the local
/// Docker Compose PostgreSQL defaults and the documented static development tenant.
/// </remarks>
public sealed class EngiFlowDbContextFactory : IDesignTimeDbContextFactory<EngiFlowDbContext>
{
    private const string DefaultConnectionString =
        "Host=localhost;Port=5432;Database=engiflow;Username=engiflow;Password=engiflow_dev_password";

    /// <inheritdoc />
    public EngiFlowDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? Environment.GetEnvironmentVariable("ENGIFLOW_DB_CONNECTION")
            ?? DefaultConnectionString;

        var tenantId = StaticTenantProvider.FromConfigurationValue(
            Environment.GetEnvironmentVariable("EngiFlow__Tenancy__CurrentCompanyId"));

        var options = new DbContextOptionsBuilder<EngiFlowDbContext>()
            .UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsAssembly(typeof(EngiFlowDbContext).Assembly.FullName))
            .Options;

        return new EngiFlowDbContext(options, new StaticTenantProvider(tenantId));
    }
}
