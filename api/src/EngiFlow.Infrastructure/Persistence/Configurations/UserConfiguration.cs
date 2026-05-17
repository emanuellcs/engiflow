using EngiFlow.Domain.Users;
using EngiFlow.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EngiFlow.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configures relational persistence for company-scoped users.
/// </summary>
/// <remarks>
/// User rows carry both their own identifier and the tenant identifier. Composite
/// alternate keys allow ECO and audit-event relationships to prove that actors belong
/// to the same company as the records they affect.
/// </remarks>
internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable(
            "users",
            table => table.HasCheckConstraint(
                "ck_users_role",
                "\"role\" IN ('Owner', 'Administrator', 'Approver', 'Requester', 'Viewer')"));

        builder.HasKey(user => user.Id);

        builder.HasAlternateKey(user => new { user.Id, user.CompanyId })
            .HasName("ak_users_id_company_id");

        builder.Property(user => user.Id)
            .HasColumnName("id")
            .HasConversion(StronglyTypedIdConverters.UserId)
            .ValueGeneratedNever();

        builder.Property(user => user.CompanyId)
            .HasColumnName("company_id")
            .HasConversion(StronglyTypedIdConverters.CompanyId)
            .IsRequired();

        builder.Property(user => user.Email)
            .HasColumnName("email")
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(user => user.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(user => user.Role)
            .HasColumnName("role")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(user => user.PasswordHash)
            .HasColumnName("password_hash")
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(user => user.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(user => user.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(user => user.DeactivatedAt)
            .HasColumnName("deactivated_at")
            .HasColumnType("timestamp with time zone");

        builder.HasIndex(user => new { user.CompanyId, user.Email })
            .IsUnique()
            .HasDatabaseName("ux_users_company_id_email");

        builder.HasIndex(user => user.Email)
            .IsUnique()
            .HasDatabaseName("ux_users_email");

        builder.HasIndex(user => new { user.CompanyId, user.Role })
            .HasDatabaseName("ix_users_company_id_role");
    }
}
