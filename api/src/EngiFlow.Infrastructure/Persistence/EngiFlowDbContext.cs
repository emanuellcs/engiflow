using System.Linq.Expressions;
using EngiFlow.Application.Abstractions.Tenancy;
using EngiFlow.Domain.Abstractions;
using EngiFlow.Domain.Companies;
using EngiFlow.Domain.Ecos;
using EngiFlow.Domain.Users;
using EngiFlow.Domain.ValueObjects;
using EngiFlow.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace EngiFlow.Infrastructure.Persistence;

/// <summary>
/// Entity Framework Core unit-of-work for EngiFlow persistence.
/// </summary>
/// <remarks>
/// This context is responsible for translating the rich domain model into relational
/// storage while enforcing tenant isolation at the persistence boundary. Global query
/// filters reduce the chance of accidental cross-company reads, and write validation
/// prevents manually attached tenant-scoped entities from being saved under the wrong
/// tenant.
/// </remarks>
public sealed class EngiFlowDbContext : DbContext
{
    private readonly ITenantProvider _tenantProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="EngiFlowDbContext"/> class.
    /// </summary>
    /// <param name="options">The EF Core options configured by the composition root.</param>
    /// <param name="tenantProvider">The provider for the current company tenant.</param>
    public EngiFlowDbContext(DbContextOptions<EngiFlowDbContext> options, ITenantProvider tenantProvider)
        : base(options)
    {
        _tenantProvider = tenantProvider;
    }

    /// <summary>
    /// Gets the current tenant used by global query filters.
    /// </summary>
    public CompanyId CurrentCompanyId => _tenantProvider.CurrentCompanyId;

    /// <summary>
    /// Gets the companies table.
    /// </summary>
    public DbSet<Company> Companies => Set<Company>();

    /// <summary>
    /// Gets tenant-scoped company workflow settings.
    /// </summary>
    public DbSet<CompanySettings> CompanySettings => Set<CompanySettings>();

    /// <summary>
    /// Gets the users table.
    /// </summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>
    /// Gets the engineering change orders table.
    /// </summary>
    public DbSet<EngineeringChangeOrder> EngineeringChangeOrders => Set<EngineeringChangeOrder>();

    /// <summary>
    /// Gets the immutable ECO audit events table.
    /// </summary>
    public DbSet<EcoEvent> EcoEvents => Set<EcoEvent>();

    /// <summary>
    /// Gets ECO comments.
    /// </summary>
    public DbSet<EcoComment> EcoComments => Set<EcoComment>();

    /// <summary>
    /// Gets ECO affected items.
    /// </summary>
    public DbSet<EcoAffectedItem> EcoAffectedItems => Set<EcoAffectedItem>();

    /// <summary>
    /// Gets ECO approvals.
    /// </summary>
    public DbSet<EcoApproval> EcoApprovals => Set<EcoApproval>();

    /// <summary>
    /// Gets ECO attachment metadata records.
    /// </summary>
    public DbSet<EcoAttachment> EcoAttachments => Set<EcoAttachment>();

    /// <inheritdoc />
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ValidateTenantScopedWrites();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    /// <inheritdoc />
    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        ValidateTenantScopedWrites();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new CompanyConfiguration());
        modelBuilder.ApplyConfiguration(new CompanySettingsConfiguration());
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new EngineeringChangeOrderConfiguration());
        modelBuilder.ApplyConfiguration(new EcoEventConfiguration());
        modelBuilder.ApplyConfiguration(new EcoCommentConfiguration());
        modelBuilder.ApplyConfiguration(new EcoAffectedItemConfiguration());
        modelBuilder.ApplyConfiguration(new EcoApprovalConfiguration());
        modelBuilder.ApplyConfiguration(new EcoAttachmentConfiguration());

        ApplyTenantQueryFilters(modelBuilder);
    }

    /// <summary>
    /// Adds a tenant predicate to every mapped entity that implements <see cref="ITenantScoped"/>.
    /// </summary>
    /// <param name="modelBuilder">The model builder being configured.</param>
    /// <remarks>
    /// Query filters are a defense-in-depth measure: they make ordinary LINQ queries tenant
    /// aware by default, but they are not the only control. Save-time validation remains
    /// necessary because filters do not protect manually attached entities or writes.
    /// </remarks>
    private void ApplyTenantQueryFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(ITenantScoped).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            var parameter = Expression.Parameter(entityType.ClrType, "entity");
            var companyIdProperty = Expression.Call(
                typeof(EF),
                nameof(EF.Property),
                [typeof(CompanyId)],
                parameter,
                Expression.Constant(nameof(ITenantScoped.CompanyId)));
            var currentCompanyId = Expression.Property(Expression.Constant(this), nameof(CurrentCompanyId));
            var tenantPredicate = Expression.Equal(companyIdProperty, currentCompanyId);
            Expression predicate = tenantPredicate;

            if (entityType.ClrType == typeof(User))
            {
                var isActiveProperty = Expression.Call(
                    typeof(EF),
                    nameof(EF.Property),
                    [typeof(bool)],
                    parameter,
                    Expression.Constant(nameof(User.IsActive)));
                predicate = Expression.AndAlso(predicate, isActiveProperty);
            }

            var lambda = Expression.Lambda(predicate, parameter);

            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
        }
    }

    /// <summary>
    /// Rejects tenant-scoped writes that do not belong to the current tenant.
    /// </summary>
    /// <remarks>
    /// Global filters do not apply to all write paths, especially when entities are attached
    /// directly by key. This check ensures inserts, updates, and deletes still honor the
    /// tenant boundary before any SQL is sent to PostgreSQL.
    /// </remarks>
    private void ValidateTenantScopedWrites()
    {
        var tenantScopedEntries = ChangeTracker
            .Entries<ITenantScoped>()
            .Where(entry => entry.State is not EntityState.Detached and not EntityState.Unchanged)
            .ToArray();

        if (tenantScopedEntries.Length == 0 || IsNewTenantBootstrapGraph(tenantScopedEntries))
        {
            return;
        }

        var currentCompanyId = CurrentCompanyId;

        foreach (var entry in tenantScopedEntries)
        {
            if (entry.Entity.CompanyId != currentCompanyId)
            {
                throw new InvalidOperationException(
                    $"Cannot save {entry.Metadata.ClrType.Name} for tenant '{entry.Entity.CompanyId}' while the current tenant is '{currentCompanyId}'.");
            }
        }
    }

    private bool IsNewTenantBootstrapGraph(IReadOnlyCollection<EntityEntry<ITenantScoped>> tenantScopedEntries)
    {
        var addedCompanyIds = ChangeTracker
            .Entries<Company>()
            .Where(entry => entry.State == EntityState.Added)
            .Select(entry => entry.Entity.Id)
            .ToHashSet();

        return addedCompanyIds.Count > 0
            && tenantScopedEntries.All(entry =>
                entry.State == EntityState.Added
                && addedCompanyIds.Contains(entry.Entity.CompanyId));
    }
}
