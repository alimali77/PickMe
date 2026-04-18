using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PickMe.Domain.Entities;

namespace PickMe.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("Users");
        b.HasKey(x => x.Id);
        b.Property(x => x.Email).IsRequired().HasMaxLength(256);
        b.HasIndex(x => x.Email).IsUnique();
        b.Property(x => x.PasswordHash).IsRequired().HasMaxLength(256);
        b.Property(x => x.Role).HasConversion<int>().IsRequired();
        b.Property(x => x.EmailConfirmed).IsRequired();
        b.Property(x => x.IsActive).IsRequired();
        b.Property(x => x.FailedLoginAttempts).IsRequired();
        b.Property(x => x.LastLoginAtUtc).HasColumnType("datetime2(3)");
        b.Property(x => x.LockedUntilUtc).HasColumnType("datetime2(3)");
        b.Property(x => x.CreatedAtUtc).HasColumnType("datetime2(3)").IsRequired();
        b.Property(x => x.UpdatedAtUtc).HasColumnType("datetime2(3)");
    }
}

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> b)
    {
        b.ToTable("Customers");
        b.HasKey(x => x.Id);
        b.Property(x => x.FirstName).IsRequired().HasMaxLength(50);
        b.Property(x => x.LastName).IsRequired().HasMaxLength(50);
        b.Property(x => x.PhoneNumber).IsRequired().HasMaxLength(25);
        b.Property(x => x.KvkkAccepted).IsRequired();
        b.Property(x => x.KvkkAcceptedAtUtc).HasColumnType("datetime2(3)");
        b.Property(x => x.CreatedAtUtc).HasColumnType("datetime2(3)").IsRequired();
        b.Property(x => x.UpdatedAtUtc).HasColumnType("datetime2(3)");
        b.HasIndex(x => x.UserId).IsUnique();
        b.HasOne(x => x.User)
            .WithOne()
            .HasForeignKey<Customer>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class DriverConfiguration : IEntityTypeConfiguration<Driver>
{
    public void Configure(EntityTypeBuilder<Driver> b)
    {
        b.ToTable("Drivers");
        b.HasKey(x => x.Id);
        b.Property(x => x.FirstName).IsRequired().HasMaxLength(50);
        b.Property(x => x.LastName).IsRequired().HasMaxLength(50);
        b.Property(x => x.PhoneNumber).IsRequired().HasMaxLength(25);
        b.Property(x => x.Status).HasConversion<int>().IsRequired();
        b.Property(x => x.AverageRating).HasColumnType("decimal(3,2)").IsRequired();
        b.Property(x => x.TotalTrips).IsRequired();
        b.Property(x => x.MustChangePassword).IsRequired();
        b.Property(x => x.IsDeleted).IsRequired();
        b.Property(x => x.DeletedAtUtc).HasColumnType("datetime2(3)");
        b.Property(x => x.CreatedAtUtc).HasColumnType("datetime2(3)").IsRequired();
        b.Property(x => x.UpdatedAtUtc).HasColumnType("datetime2(3)");
        b.HasIndex(x => x.UserId).IsUnique();
        b.HasOne(x => x.User)
            .WithOne()
            .HasForeignKey<Driver>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}

public sealed class AdminConfiguration : IEntityTypeConfiguration<Admin>
{
    public void Configure(EntityTypeBuilder<Admin> b)
    {
        b.ToTable("Admins");
        b.HasKey(x => x.Id);
        b.Property(x => x.FullName).IsRequired().HasMaxLength(100);
        b.Property(x => x.CreatedAtUtc).HasColumnType("datetime2(3)").IsRequired();
        b.Property(x => x.UpdatedAtUtc).HasColumnType("datetime2(3)");
        b.HasIndex(x => x.UserId).IsUnique();
        b.HasOne(x => x.User)
            .WithOne()
            .HasForeignKey<Admin>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
