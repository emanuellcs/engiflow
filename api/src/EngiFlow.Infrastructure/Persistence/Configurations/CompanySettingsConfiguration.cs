using EngiFlow.Domain.Companies;
using EngiFlow.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EngiFlow.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configures tenant-scoped company workflow settings.
/// </summary>
internal sealed class CompanySettingsConfiguration : IEntityTypeConfiguration<CompanySettings>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<CompanySettings> builder)
    {
        builder.ToTable(
            "company_settings",
            table => table.HasCheckConstraint(
                "ck_company_settings_min_approvals_required",
                "\"min_approvals_required\" >= 1"));

        builder.HasKey(settings => settings.CompanyId);

        builder.Property(settings => settings.CompanyId)
            .HasColumnName("company_id")
            .HasConversion(StronglyTypedIdConverters.CompanyId)
            .ValueGeneratedNever();

        builder.Property(settings => settings.MinApprovalsRequired)
            .HasColumnName("min_approvals_required")
            .HasDefaultValue(1)
            .IsRequired();

        builder.Property(settings => settings.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(settings => settings.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasOne<Company>()
            .WithOne()
            .HasForeignKey<CompanySettings>(settings => settings.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
