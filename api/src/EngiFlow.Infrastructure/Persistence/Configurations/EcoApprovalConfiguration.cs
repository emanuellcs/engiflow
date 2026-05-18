using EngiFlow.Domain.Ecos;
using EngiFlow.Domain.Users;
using EngiFlow.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EngiFlow.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configures relational persistence for ECO approval decisions.
/// </summary>
internal sealed class EcoApprovalConfiguration : IEntityTypeConfiguration<EcoApproval>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<EcoApproval> builder)
    {
        builder.ToTable(
            "eco_approvals",
            table => table.HasCheckConstraint(
                "ck_eco_approvals_decision",
                "\"decision\" IN ('Approve', 'RequestChanges')"));

        builder.HasKey(approval => approval.Id);

        builder.Property(approval => approval.Id)
            .HasColumnName("id")
            .HasConversion(StronglyTypedIdConverters.EcoApprovalId)
            .ValueGeneratedNever();

        builder.Property(approval => approval.CompanyId)
            .HasColumnName("company_id")
            .HasConversion(StronglyTypedIdConverters.CompanyId)
            .IsRequired();

        builder.Property(approval => approval.EngineeringChangeOrderId)
            .HasColumnName("engineering_change_order_id")
            .HasConversion(StronglyTypedIdConverters.EngineeringChangeOrderId)
            .IsRequired();

        builder.Property(approval => approval.ApproverUserId)
            .HasColumnName("approver_user_id")
            .HasConversion(StronglyTypedIdConverters.UserId)
            .IsRequired();

        builder.Property(approval => approval.Decision)
            .HasColumnName("decision")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(approval => approval.ReviewRound)
            .HasColumnName("review_round")
            .IsRequired();

        builder.Property(approval => approval.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(approval => approval.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(approval => new
            {
                approval.CompanyId,
                approval.EngineeringChangeOrderId,
                approval.ReviewRound,
                approval.ApproverUserId
            })
            .IsUnique()
            .HasDatabaseName("ux_eco_approvals_company_id_eco_id_round_approver");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(approval => new { approval.ApproverUserId, approval.CompanyId })
            .HasPrincipalKey(user => new { user.Id, user.CompanyId })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
