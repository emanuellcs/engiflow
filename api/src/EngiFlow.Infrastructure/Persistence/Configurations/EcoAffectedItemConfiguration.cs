using EngiFlow.Domain.Ecos;
using EngiFlow.Domain.Users;
using EngiFlow.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EngiFlow.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configures relational persistence for ECO affected items.
/// </summary>
internal sealed class EcoAffectedItemConfiguration : IEntityTypeConfiguration<EcoAffectedItem>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<EcoAffectedItem> builder)
    {
        builder.ToTable(
            "eco_affected_items",
            table => table.HasCheckConstraint(
                "ck_eco_affected_items_action",
                "\"action\" IN ('Add', 'Modify', 'Remove')"));

        builder.HasKey(item => item.Id);

        builder.Property(item => item.Id)
            .HasColumnName("id")
            .HasConversion(StronglyTypedIdConverters.EcoAffectedItemId)
            .ValueGeneratedNever();

        builder.Property(item => item.CompanyId)
            .HasColumnName("company_id")
            .HasConversion(StronglyTypedIdConverters.CompanyId)
            .IsRequired();

        builder.Property(item => item.EngineeringChangeOrderId)
            .HasColumnName("engineering_change_order_id")
            .HasConversion(StronglyTypedIdConverters.EngineeringChangeOrderId)
            .IsRequired();

        builder.Property(item => item.PartNumber)
            .HasColumnName("part_number")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(item => item.Description)
            .HasColumnName("description")
            .HasMaxLength(1_000)
            .IsRequired();

        builder.Property(item => item.CurrentRevision)
            .HasColumnName("current_revision")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(item => item.NewRevision)
            .HasColumnName("new_revision")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(item => item.Action)
            .HasColumnName("action")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(item => item.CreatedByUserId)
            .HasColumnName("created_by_user_id")
            .HasConversion(StronglyTypedIdConverters.UserId)
            .IsRequired();

        builder.Property(item => item.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(item => new { item.CompanyId, item.EngineeringChangeOrderId, item.PartNumber })
            .HasDatabaseName("ix_eco_affected_items_company_id_eco_id_part_number");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(item => new { item.CreatedByUserId, item.CompanyId })
            .HasPrincipalKey(user => new { user.Id, user.CompanyId })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
