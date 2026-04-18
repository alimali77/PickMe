using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PickMe.Application.Abstractions;
using PickMe.Domain;
using PickMe.Domain.Entities;

namespace PickMe.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    /// <summary>
    /// Startup'ta:
    /// 1) Bekleyen migration'ları uygular.
    /// 2) SEED_ADMIN_EMAIL env var'ı varsa ilk admin hesabını + (yoksa) AdminNotificationRecipient kaydını oluşturur.
    /// Brief: "İlk admin migration seed ile oluşturulur (env var'dan). Sonraki adminleri mevcut admin ekler."
    /// </summary>
    public static async Task InitializeAsync(IServiceProvider services, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        var pending = await db.Database.GetPendingMigrationsAsync(ct);
        if (pending.Any())
        {
            logger.LogInformation("Applying {Count} pending migration(s)...", pending.Count());
            await db.Database.MigrateAsync(ct);
        }

        var seedEmail = (config["SEED_ADMIN_EMAIL"] ?? Environment.GetEnvironmentVariable("SEED_ADMIN_EMAIL"))?.Trim().ToLowerInvariant();
        var seedPassword = config["SEED_ADMIN_PASSWORD"] ?? Environment.GetEnvironmentVariable("SEED_ADMIN_PASSWORD");
        var seedFullName = config["SEED_ADMIN_FULL_NAME"] ?? Environment.GetEnvironmentVariable("SEED_ADMIN_FULL_NAME") ?? "Sistem Yöneticisi";

        if (!string.IsNullOrWhiteSpace(seedEmail) && !string.IsNullOrWhiteSpace(seedPassword))
        {
            var exists = await db.Users.AnyAsync(u => u.Email == seedEmail, ct);
            if (!exists)
            {
                var userId = Guid.NewGuid();
                var user = User.Create(userId, seedEmail, hasher.Hash(seedPassword), UserRole.Admin);
                user.ConfirmEmail();
                var admin = Admin.Create(Guid.NewGuid(), userId, seedFullName);
                db.Users.Add(user);
                db.Admins.Add(admin);

                // En az bir AdminNotificationRecipient oluştur (yeni rezervasyon mailinin gitmesi için).
                if (!await db.AdminNotificationRecipients.AnyAsync(r => r.IsActive, ct))
                {
                    db.AdminNotificationRecipients.Add(AdminNotificationRecipient.Create(Guid.NewGuid(), seedEmail));
                }

                await db.SaveChangesAsync(ct);
                logger.LogInformation("Seeded initial admin: {Email}", seedEmail);
            }
            else
            {
                logger.LogDebug("Seed admin already exists: {Email}", seedEmail);
            }
        }
        else
        {
            logger.LogWarning("SEED_ADMIN_EMAIL or SEED_ADMIN_PASSWORD not set — skipping admin seed.");
        }
    }
}
