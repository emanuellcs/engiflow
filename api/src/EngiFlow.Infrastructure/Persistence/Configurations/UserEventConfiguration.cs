using EngiFlow.Domain.Users;
using EngiFlow.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EngiFlow.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configures relational persistence for immutable user lifecycle audit events.
/// </summary>
internal sealed class UserEventConfiguration : IEntityTypeConfiguration<UserEvent>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<UserEvent> builder)
    {
        builder.ToTable(
            "user_events",
            table => table.HasCheckConstraint(
                "ck_user_events_event_type",
                "\"event_type\" IN ('UserInvited', 'UserActivated', 'UserDeactivated', 'UserReactivated', 'PasswordResetRequested', 'FirstAccessInvitationResent')"));

        builder.HasKey(userEvent => userEvent.Id);

        builder.Property(userEvent => userEvent.Id)
            .HasColumnName("id")
            .HasConversion(StronglyTypedIdConverters.UserEventId)
            .ValueGeneratedNever();

        builder.Property(userEvent => userEvent.CompanyId)
            .HasColumnName("company_id")
            .HasConversion(StronglyTypedIdConverters.CompanyId)
            .IsRequired();

        builder.Property(userEvent => userEvent.UserId)
            .HasColumnName("user_id")
            .HasConversion(StronglyTypedIdConverters.UserId)
            .IsRequired();

        builder.Property(userEvent => userEvent.ActorId)
            .HasColumnName("actor_id")
            .HasConversion(StronglyTypedIdConverters.UserId)
            .IsRequired();

        builder.Property(userEvent => userEvent.EventType)
            .HasColumnName("event_type")
            .HasConversion<string>()
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(userEvent => userEvent.OccurredAt)
            .HasColumnName("occurred_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(userEvent => userEvent.Reason)
            .HasColumnName("reason")
            .HasMaxLength(1000);

        builder.HasIndex(userEvent => new { userEvent.CompanyId, userEvent.UserId, userEvent.OccurredAt })
            .HasDatabaseName("ix_user_events_company_id_user_id_occurred_at");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(userEvent => new { userEvent.UserId, userEvent.CompanyId })
            .HasPrincipalKey(user => new { user.Id, user.CompanyId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(userEvent => new { userEvent.ActorId, userEvent.CompanyId })
            .HasPrincipalKey(user => new { user.Id, user.CompanyId })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
