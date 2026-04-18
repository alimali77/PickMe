using Microsoft.EntityFrameworkCore;
using PickMe.Application.Abstractions;
using PickMe.Domain.Entities;

namespace PickMe.Infrastructure.Persistence;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Driver> Drivers => Set<Driver>();
    public DbSet<Admin> Admins => Set<Admin>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<Rating> Ratings => Set<Rating>();
    public DbSet<EmailVerificationToken> EmailVerificationTokens => Set<EmailVerificationToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AdminNotificationRecipient> AdminNotificationRecipients => Set<AdminNotificationRecipient>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();
    public DbSet<Faq> Faqs => Set<Faq>();
    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();
    public DbSet<EmailLog> EmailLogs => Set<EmailLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
