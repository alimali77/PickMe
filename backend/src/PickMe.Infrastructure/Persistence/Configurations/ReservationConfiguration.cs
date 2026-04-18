using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PickMe.Domain.Entities;

namespace PickMe.Infrastructure.Persistence.Configurations;

public sealed class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> b)
    {
        b.ToTable("Reservations");
        b.HasKey(x => x.Id);
        b.Property(x => x.ServiceType).HasConversion<int>().IsRequired();
        b.Property(x => x.Status).HasConversion<int>().IsRequired();
        b.Property(x => x.CancelledBy).HasConversion<int?>();
        b.Property(x => x.ReservationDateTimeUtc).HasColumnType("datetime2(3)").IsRequired();
        b.Property(x => x.Address).IsRequired().HasMaxLength(300);
        b.Property(x => x.Lat).IsRequired();
        b.Property(x => x.Lng).IsRequired();
        b.Property(x => x.Note).HasMaxLength(500);
        b.Property(x => x.CancellationReason).HasMaxLength(1000);
        b.Property(x => x.AssignedAtUtc).HasColumnType("datetime2(3)");
        b.Property(x => x.StartedAtUtc).HasColumnType("datetime2(3)");
        b.Property(x => x.CompletedAtUtc).HasColumnType("datetime2(3)");
        b.Property(x => x.CancelledAtUtc).HasColumnType("datetime2(3)");
        b.Property(x => x.CreatedAtUtc).HasColumnType("datetime2(3)").IsRequired();
        b.Property(x => x.UpdatedAtUtc).HasColumnType("datetime2(3)");
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Driver)
            .WithMany()
            .HasForeignKey(x => x.DriverId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => new { x.Status, x.ReservationDateTimeUtc });
        b.HasIndex(x => x.CustomerId);
        b.HasIndex(x => x.DriverId);
    }
}

public sealed class RatingConfiguration : IEntityTypeConfiguration<Rating>
{
    public void Configure(EntityTypeBuilder<Rating> b)
    {
        b.ToTable("Ratings");
        b.HasKey(x => x.Id);
        b.Property(x => x.Score).IsRequired();
        b.Property(x => x.Comment).HasMaxLength(500);
        b.Property(x => x.IsFlagged).IsRequired();
        b.Property(x => x.FlaggedReason).HasMaxLength(500);
        b.Property(x => x.CreatedAtUtc).HasColumnType("datetime2(3)").IsRequired();
        b.Property(x => x.UpdatedAtUtc).HasColumnType("datetime2(3)");

        b.HasOne(x => x.Reservation)
            .WithOne()
            .HasForeignKey<Rating>(x => x.ReservationId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => x.ReservationId).IsUnique();
        b.HasIndex(x => x.DriverId);
        b.HasIndex(x => x.CustomerId);
    }
}

public sealed class EmailVerificationTokenConfiguration : IEntityTypeConfiguration<EmailVerificationToken>
{
    public void Configure(EntityTypeBuilder<EmailVerificationToken> b)
    {
        b.ToTable("EmailVerificationTokens");
        b.HasKey(x => x.Id);
        b.Property(x => x.TokenHash).IsRequired().HasMaxLength(128);
        b.HasIndex(x => x.TokenHash).IsUnique();
        b.Property(x => x.ExpiresAtUtc).HasColumnType("datetime2(3)").IsRequired();
        b.Property(x => x.UsedAtUtc).HasColumnType("datetime2(3)");
        b.Property(x => x.CreatedAtUtc).HasColumnType("datetime2(3)").IsRequired();
        b.Property(x => x.UpdatedAtUtc).HasColumnType("datetime2(3)");
        b.HasIndex(x => x.UserId);
    }
}

public sealed class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
{
    public void Configure(EntityTypeBuilder<PasswordResetToken> b)
    {
        b.ToTable("PasswordResetTokens");
        b.HasKey(x => x.Id);
        b.Property(x => x.TokenHash).IsRequired().HasMaxLength(128);
        b.HasIndex(x => x.TokenHash).IsUnique();
        b.Property(x => x.ExpiresAtUtc).HasColumnType("datetime2(3)").IsRequired();
        b.Property(x => x.UsedAtUtc).HasColumnType("datetime2(3)");
        b.Property(x => x.CreatedAtUtc).HasColumnType("datetime2(3)").IsRequired();
        b.Property(x => x.UpdatedAtUtc).HasColumnType("datetime2(3)");
        b.HasIndex(x => x.UserId);
    }
}

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> b)
    {
        b.ToTable("RefreshTokens");
        b.HasKey(x => x.Id);
        b.Property(x => x.TokenHash).IsRequired().HasMaxLength(128);
        b.HasIndex(x => x.TokenHash).IsUnique();
        b.Property(x => x.ExpiresAtUtc).HasColumnType("datetime2(3)").IsRequired();
        b.Property(x => x.RevokedAtUtc).HasColumnType("datetime2(3)");
        b.Property(x => x.CreatedAtUtc).HasColumnType("datetime2(3)").IsRequired();
        b.Property(x => x.UpdatedAtUtc).HasColumnType("datetime2(3)");
        b.HasIndex(x => x.UserId);
    }
}
