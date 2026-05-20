using EngiFlow.Domain.Users;
using EngiFlow.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EngiFlow.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configures relational persistence for one-time password setup and reset tokens.
/// </summary>
internal sealed class PasswordSetupTokenConfiguration : IEntityTypeConfiguration<PasswordSetupToken>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PasswordSetupToken> builder)
    {
        builder.ToTable(
            "password_setup_tokens",
            table => table.HasCheckConstraint(
                "ck_password_setup_tokens_purpose",
                "\"purpose\" IN ('Invitation', 'Reset')"));

        builder.HasKey(token => token.Id);

        builder.Property(token => token.Id)
            .HasColumnName("id")
            .HasConversion(StronglyTypedIdConverters.PasswordSetupTokenId)
            .ValueGeneratedNever();

        builder.Property(token => token.UserId)
            .HasColumnName("user_id")
            .HasConversion(StronglyTypedIdConverters.UserId)
            .IsRequired();

        builder.Property(token => token.Purpose)
            .HasColumnName("purpose")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(token => token.TokenHash)
            .HasColumnName("token_hash")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(token => token.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(token => token.ExpiresAt)
            .HasColumnName("expires_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(token => token.ConsumedAt)
            .HasColumnName("consumed_at")
            .HasColumnType("timestamp with time zone");

        builder.HasIndex(token => token.TokenHash)
            .IsUnique()
            .HasDatabaseName("ux_password_setup_tokens_token_hash");

        builder.HasIndex(token => new { token.UserId, token.Purpose, token.ExpiresAt })
            .HasDatabaseName("ix_password_setup_tokens_user_id_purpose_expires_at");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
