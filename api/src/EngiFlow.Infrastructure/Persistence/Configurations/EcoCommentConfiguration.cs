using EngiFlow.Domain.Ecos;
using EngiFlow.Domain.Users;
using EngiFlow.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EngiFlow.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configures relational persistence for ECO timeline comments.
/// </summary>
internal sealed class EcoCommentConfiguration : IEntityTypeConfiguration<EcoComment>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<EcoComment> builder)
    {
        builder.ToTable("eco_comments");

        builder.HasKey(comment => comment.Id);

        builder.Property(comment => comment.Id)
            .HasColumnName("id")
            .HasConversion(StronglyTypedIdConverters.EcoCommentId)
            .ValueGeneratedNever();

        builder.Property(comment => comment.CompanyId)
            .HasColumnName("company_id")
            .HasConversion(StronglyTypedIdConverters.CompanyId)
            .IsRequired();

        builder.Property(comment => comment.EngineeringChangeOrderId)
            .HasColumnName("engineering_change_order_id")
            .HasConversion(StronglyTypedIdConverters.EngineeringChangeOrderId)
            .IsRequired();

        builder.Property(comment => comment.AuthorUserId)
            .HasColumnName("author_user_id")
            .HasConversion(StronglyTypedIdConverters.UserId)
            .IsRequired();

        builder.Property(comment => comment.Body)
            .HasColumnName("body")
            .HasMaxLength(4_000)
            .IsRequired();

        builder.Property(comment => comment.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(comment => new { comment.CompanyId, comment.EngineeringChangeOrderId, comment.CreatedAt })
            .HasDatabaseName("ix_eco_comments_company_id_eco_id_created_at");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(comment => new { comment.AuthorUserId, comment.CompanyId })
            .HasPrincipalKey(user => new { user.Id, user.CompanyId })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
