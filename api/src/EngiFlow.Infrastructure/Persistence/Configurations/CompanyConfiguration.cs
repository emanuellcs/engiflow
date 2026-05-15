using EngiFlow.Domain.Companies;
using EngiFlow.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EngiFlow.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configures relational persistence for company tenants.
/// </summary>
/// <remarks>
/// Companies are the tenant roots rather than tenant-scoped children. They are not
/// protected by the global tenant filter because onboarding, administration, and future
/// billing workflows must be able to address tenant records explicitly.
/// </remarks>
internal sealed class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("companies");

        builder.HasKey(company => company.Id);

        builder.Property(company => company.Id)
            .HasColumnName("id")
            .HasConversion(StronglyTypedIdConverters.CompanyId)
            .ValueGeneratedNever();

        builder.Property(company => company.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(company => company.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(company => company.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(company => company.DeactivatedAt)
            .HasColumnName("deactivated_at")
            .HasColumnType("timestamp with time zone");

        builder.HasIndex(company => company.IsActive)
            .HasDatabaseName("ix_companies_is_active");

        builder.HasMany(company => company.Users)
            .WithOne()
            .HasForeignKey(user => user.CompanyId)
            .HasPrincipalKey(company => company.Id)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(company => company.Users)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
