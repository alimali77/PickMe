using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PickMe.Infrastructure.Persistence;

/// <summary>
/// dotnet-ef CLI migration komutları için design-time factory.
/// Runtime'da kullanılmaz; ApplicationDbContext DI üzerinden resolve edilir.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
            ?? @"Server=(localdb)\MSSQLLocalDB;Database=PickMeDB;Integrated Security=true;TrustServerCertificate=true;";

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(connectionString, sql => sql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName))
            .Options;

        return new ApplicationDbContext(options);
    }
}
