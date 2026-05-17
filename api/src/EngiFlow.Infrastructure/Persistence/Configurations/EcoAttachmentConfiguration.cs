using EngiFlow.Domain.Ecos;
using EngiFlow.Domain.Users;
using EngiFlow.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EngiFlow.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configures relational persistence for ECO attachment metadata.
/// </summary>
internal sealed class EcoAttachmentConfiguration : IEntityTypeConfiguration<EcoAttachment>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<EcoAttachment> builder)
    {
        builder.ToTable("eco_attachments");

        builder.HasKey(attachment => attachment.Id);

        builder.Property(attachment => attachment.Id)
            .HasColumnName("id")
            .HasConversion(StronglyTypedIdConverters.EcoAttachmentId)
            .ValueGeneratedNever();

        builder.Property(attachment => attachment.CompanyId)
            .HasColumnName("company_id")
            .HasConversion(StronglyTypedIdConverters.CompanyId)
            .IsRequired();

        builder.Property(attachment => attachment.EngineeringChangeOrderId)
            .HasColumnName("engineering_change_order_id")
            .HasConversion(StronglyTypedIdConverters.EngineeringChangeOrderId)
            .IsRequired();

        builder.Property(attachment => attachment.FileName)
            .HasColumnName("file_name")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(attachment => attachment.FileSize)
            .HasColumnName("file_size")
            .IsRequired();

        builder.Property(attachment => attachment.ObjectKey)
            .HasColumnName("object_key")
            .HasMaxLength(1_024)
            .IsRequired();

        builder.Property(attachment => attachment.MimeType)
            .HasColumnName("mime_type")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(attachment => attachment.UploadedByUserId)
            .HasColumnName("uploaded_by_user_id")
            .HasConversion(StronglyTypedIdConverters.UserId)
            .IsRequired();

        builder.Property(attachment => attachment.UploadedAt)
            .HasColumnName("uploaded_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(attachment => new { attachment.CompanyId, attachment.EngineeringChangeOrderId, attachment.UploadedAt })
            .HasDatabaseName("ix_eco_attachments_company_id_eco_id_uploaded_at");

        builder.HasIndex(attachment => new { attachment.CompanyId, attachment.ObjectKey })
            .IsUnique()
            .HasDatabaseName("ux_eco_attachments_company_id_object_key");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(attachment => new { attachment.UploadedByUserId, attachment.CompanyId })
            .HasPrincipalKey(user => new { user.Id, user.CompanyId })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
