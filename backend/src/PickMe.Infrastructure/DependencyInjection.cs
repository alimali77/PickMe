using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PickMe.Application.Abstractions;
using PickMe.Application.Auth;
using PickMe.Infrastructure.Email;
using PickMe.Infrastructure.Persistence;
using PickMe.Infrastructure.Security;

namespace PickMe.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        // DB
        var conn = config["DB_CONNECTION_STRING"]
            ?? Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
            ?? throw new InvalidOperationException("DB_CONNECTION_STRING ayarlanmamış.");

        services.AddDbContext<ApplicationDbContext>(opt =>
            opt.UseSqlServer(conn, sql =>
                sql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        // Security
        services.Configure<JwtOptions>(o =>
        {
            o.Secret = config["JWT_SECRET"] ?? Environment.GetEnvironmentVariable("JWT_SECRET") ?? throw new InvalidOperationException("JWT_SECRET ayarlanmamış.");
            o.Issuer = config["JWT_ISSUER"] ?? "pickme-api";
            o.Audience = config["JWT_AUDIENCE"] ?? "pickme-web";
            o.AccessTtlMinutes = int.Parse(config["JWT_ACCESS_TTL_MINUTES"] ?? "60");
            o.RefreshTtlDays = int.Parse(config["JWT_REFRESH_TTL_DAYS"] ?? "7");
        });

        var bcryptCost = int.Parse(config["BCRYPT_COST"] ?? "12");
        services.AddSingleton<IPasswordHasher>(_ => new BcryptPasswordHasher(bcryptCost));
        services.AddSingleton<ITokenHasher, Sha256TokenHasher>();
        services.AddSingleton<IOpaqueTokenGenerator, OpaqueTokenGenerator>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IClock, SystemClock>();

        // Mail
        services.Configure<SmtpOptions>(o =>
        {
            o.Host = config["SMTP_HOST"] ?? "localhost";
            o.Port = int.Parse(config["SMTP_PORT"] ?? "25");
            o.User = config["SMTP_USER"];
            o.Password = config["SMTP_PASS"];
            o.FromEmail = config["SMTP_FROM"] ?? "no-reply@pickme.local";
            o.FromName = config["SMTP_FROM_NAME"] ?? "Pick Me";
            o.EnableSsl = bool.Parse(config["SMTP_ENABLE_SSL"] ?? "false");
        });
        services.AddSingleton<IEmailSender, MailKitEmailSender>();
        services.AddSingleton<IEmailQueue, BackgroundEmailQueue>();

        // Frontend URL helper (verify / reset link'leri için)
        services.AddSingleton<IFrontendUrlProvider, FrontendUrlProvider>();

        return services;
    }
}
