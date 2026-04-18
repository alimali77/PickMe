using Microsoft.EntityFrameworkCore;
using PickMe.Domain.Entities;

namespace PickMe.Application.Abstractions;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Customer> Customers { get; }
    DbSet<Driver> Drivers { get; }
    DbSet<Admin> Admins { get; }
    DbSet<Reservation> Reservations { get; }
    DbSet<Rating> Ratings { get; }
    DbSet<EmailVerificationToken> EmailVerificationTokens { get; }
    DbSet<PasswordResetToken> PasswordResetTokens { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<AdminNotificationRecipient> AdminNotificationRecipients { get; }
    DbSet<SystemSetting> SystemSettings { get; }
    DbSet<Faq> Faqs { get; }
    DbSet<ContactMessage> ContactMessages { get; }
    DbSet<EmailLog> EmailLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken ct);
}
