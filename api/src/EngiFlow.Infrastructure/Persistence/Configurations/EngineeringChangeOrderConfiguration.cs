using EngiFlow.Domain.Ecos;
using EngiFlow.Domain.Users;
using EngiFlow.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EngiFlow.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configures relational persistence for engineering change order aggregates.
/// </summary>
/// <remarks>
/// The aggregate owns lifecycle state and audit timeline creation in the domain. The
/// mapping keeps the event collection as a private-field-backed navigation while the
/// SaveChanges interceptor controls when newly pending events are flushed to storage.
/// </remarks>
internal sealed class EngineeringChangeOrderConfiguration : IEntityTypeConfiguration<EngineeringChangeOrder>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<EngineeringChangeOrder> builder)
    {
        builder.ToTable(
            "engineering_change_orders",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_engineering_change_orders_priority",
                    "\"priority\" IN ('Low', 'Medium', 'High', 'Critical')");
                table.HasCheckConstraint(
                    "ck_engineering_change_orders_status",
                    "\"status\" IN ('Draft', 'UnderReview', 'Approved', 'Canceled', 'Rejected', 'Implemented')");
            });

        builder.HasKey(eco => eco.Id);

        builder.HasAlternateKey(eco => new { eco.Id, eco.CompanyId })
            .HasName("ak_engineering_change_orders_id_company_id");

        builder.Ignore(eco => eco.PendingEvents);

        builder.Property(eco => eco.Id)
            .HasColumnName("id")
            .HasConversion(StronglyTypedIdConverters.EngineeringChangeOrderId)
            .ValueGeneratedNever();

        builder.Property(eco => eco.CompanyId)
            .HasColumnName("company_id")
            .HasConversion(StronglyTypedIdConverters.CompanyId)
            .IsRequired();

        builder.Property(eco => eco.Title)
            .HasColumnName("title")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(eco => eco.Description)
            .HasColumnName("description")
            .HasMaxLength(4_000)
            .IsRequired();

        builder.Property(eco => eco.Priority)
            .HasColumnName("priority")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(eco => eco.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(eco => eco.ReviewRound)
            .HasColumnName("review_round")
            .IsRequired();

        builder.Property(eco => eco.RowVersion)
            .IsRowVersion();

        builder.Property(eco => eco.CreatedByUserId)
            .HasColumnName("created_by_user_id")
            .HasConversion(StronglyTypedIdConverters.UserId)
            .IsRequired();

        builder.Property(eco => eco.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(eco => eco.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(eco => new { eco.CompanyId, eco.Status })
            .HasDatabaseName("ix_engineering_change_orders_company_id_status");

        builder.HasIndex(eco => new { eco.CompanyId, eco.CreatedAt })
            .HasDatabaseName("ix_engineering_change_orders_company_id_created_at");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(eco => new { eco.CreatedByUserId, eco.CompanyId })
            .HasPrincipalKey(user => new { user.Id, user.CompanyId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(eco => eco.Events)
            .WithOne()
            .HasForeignKey(ecoEvent => new { ecoEvent.EngineeringChangeOrderId, ecoEvent.CompanyId })
            .HasPrincipalKey(eco => new { eco.Id, eco.CompanyId })
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(eco => eco.Comments)
            .WithOne()
            .HasForeignKey(comment => new { comment.EngineeringChangeOrderId, comment.CompanyId })
            .HasPrincipalKey(eco => new { eco.Id, eco.CompanyId })
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(eco => eco.AffectedItems)
            .WithOne()
            .HasForeignKey(item => new { item.EngineeringChangeOrderId, item.CompanyId })
            .HasPrincipalKey(eco => new { eco.Id, eco.CompanyId })
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(eco => eco.Approvals)
            .WithOne()
            .HasForeignKey(approval => new { approval.EngineeringChangeOrderId, approval.CompanyId })
            .HasPrincipalKey(eco => new { eco.Id, eco.CompanyId })
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(eco => eco.Attachments)
            .WithOne()
            .HasForeignKey(attachment => new { attachment.EngineeringChangeOrderId, attachment.CompanyId })
            .HasPrincipalKey(eco => new { eco.Id, eco.CompanyId })
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(eco => eco.Events)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(eco => eco.Comments)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(eco => eco.AffectedItems)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(eco => eco.Approvals)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(eco => eco.Attachments)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
