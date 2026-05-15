using EngiFlow.Application.Abstractions.Security;
using EngiFlow.Domain.Companies;
using EngiFlow.Domain.Users;
using EngiFlow.Infrastructure.Persistence;
using EngiFlow.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EngiFlow.Api.Initialization;

/// <summary>
/// Applies local database migrations and creates the default development tenant and administrator.
/// </summary>
public sealed class EngiFlowDatabaseInitializer
{
    private readonly ILogger<EngiFlowDatabaseInitializer> _logger;
    private readonly IOptions<DevelopmentSeedOptions> _seedOptions;
    private readonly IServiceScopeFactory _scopeFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="EngiFlowDatabaseInitializer"/> class.
    /// </summary>
    /// <param name="scopeFactory">Factory used to create a startup initialization scope.</param>
    /// <param name="seedOptions">The configured development seed settings.</param>
    /// <param name="logger">The structured logger used for initialization diagnostics.</param>
    public EngiFlowDatabaseInitializer(
        IServiceScopeFactory scopeFactory,
        IOptions<DevelopmentSeedOptions> seedOptions,
        ILogger<EngiFlowDatabaseInitializer> logger)
    {
        _scopeFactory = scopeFactory;
        _seedOptions = seedOptions;
        _logger = logger;
    }

    /// <summary>
    /// Applies pending migrations and seeds a default administrator when the database is empty.
    /// </summary>
    /// <param name="cancellationToken">A token that can cancel initialization.</param>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<EngiFlowDbContext>>();
        var passwordHashService = scope.ServiceProvider.GetRequiredService<IPasswordHashService>();
        var seed = _seedOptions.Value;

        await using var dbContext = new EngiFlowDbContext(
            options,
            new StaticTenantProvider(
                StaticTenantProvider.DevelopmentFallbackCompanyId,
                StaticTenantProvider.DevelopmentFallbackUserId));

        if (dbContext.Database.IsRelational())
        {
            await dbContext.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
        }

        if (await dbContext.Companies.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            _logger.LogInformation("EngiFlow database already contains companies; development seed skipped.");
            return;
        }

        var company = Company.Create(seed.CompanyName);
        var admin = company.RegisterUser(
            seed.AdminEmail,
            seed.AdminDisplayName,
            UserRole.Administrator);
        admin.SetPasswordHash(passwordHashService.HashPassword(admin, seed.AdminPassword));

        await using var seedContext = new EngiFlowDbContext(
            options,
            new StaticTenantProvider(company.Id, admin.Id));
        seedContext.Companies.Add(company);
        await seedContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Seeded default EngiFlow development company and administrator user '{AdminEmail}'.",
            admin.Email);
    }
}
