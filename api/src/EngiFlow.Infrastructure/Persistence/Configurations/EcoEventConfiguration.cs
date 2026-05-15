using EngiFlow.Domain.Ecos;
using EngiFlow.Domain.Users;
using EngiFlow.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EngiFlow.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configures relational persistence for immutable ECO audit events.
/// </summary>
/// <remarks>
/// Audit events are modeled as append-only records. The database mapping keeps them
/// tenant-scoped and relates actors by both user id and company id so an audit row cannot
/// point at a user from another tenant.
/// </remarks>
internal sealed class EcoEventConfiguration : IEntityTypeConfiguration<EcoEvent>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<EcoEvent> builder)
    {
        builder.ToTable(
            "eco_events",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_eco_events_event_type",
                    "\"event_type\" IN ('Created', 'DetailsUpdated', 'SubmittedForReview', 'Approved', 'Rejected', 'Implemented')");
                table.HasCheckConstraint(
                    "ck_eco_events_old_status",
                    "\"old_status\" IS NULL OR \"old_status\" IN ('Draft', 'UnderReview', 'Approved', 'Rejected', 'Implemented')");
                table.HasCheckConstraint(
                    "ck_eco_events_new_status",
                    "\"new_status\" IS NULL OR \"new_status\" IN ('Draft', 'UnderReview', 'Approved', 'Rejected', 'Implemented')");
            });

        builder.HasKey(ecoEvent => ecoEvent.Id);

        builder.Property(ecoEvent => ecoEvent.Id)
            .HasColumnName("id")
            .HasConversion(StronglyTypedIdConverters.EcoEventId)
            .ValueGeneratedNever();

        builder.Property(ecoEvent => ecoEvent.CompanyId)
            .HasColumnName("company_id")
            .HasConversion(StronglyTypedIdConverters.CompanyId)
            .IsRequired();

        builder.Property(ecoEvent => ecoEvent.EngineeringChangeOrderId)
            .HasColumnName("engineering_change_order_id")
            .HasConversion(StronglyTypedIdConverters.EngineeringChangeOrderId)
            .IsRequired();

        builder.Property(ecoEvent => ecoEvent.ActorUserId)
            .HasColumnName("actor_user_id")
            .HasConversion(StronglyTypedIdConverters.UserId)
            .IsRequired();

        builder.Property(ecoEvent => ecoEvent.EventType)
            .HasColumnName("event_type")
            .HasConversion<string>()
            .HasMaxLength(48)
            .IsRequired();

        builder.Property(ecoEvent => ecoEvent.Description)
            .HasColumnName("description")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(ecoEvent => ecoEvent.OldStatus)
            .HasColumnName("old_status")
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(ecoEvent => ecoEvent.NewStatus)
            .HasColumnName("new_status")
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(ecoEvent => ecoEvent.OccurredAt)
            .HasColumnName("occurred_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(ecoEvent => new { ecoEvent.CompanyId, ecoEvent.EngineeringChangeOrderId, ecoEvent.OccurredAt })
            .HasDatabaseName("ix_eco_events_company_id_eco_id_occurred_at");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(ecoEvent => new { ecoEvent.ActorUserId, ecoEvent.CompanyId })
            .HasPrincipalKey(user => new { user.Id, user.CompanyId })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
