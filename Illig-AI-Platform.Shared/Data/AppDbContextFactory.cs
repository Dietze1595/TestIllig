using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Illig_AI_Platform.Shared.Data;

/// <summary>
/// Design-time factory für die EF Core CLI (dotnet ef migrations add/update).
/// Liest den Connection String aus der appsettings.json des Startup-Projekts
/// (Key "Db:ConnectionString") — dieselbe Quelle wie die laufende App.
/// Eine gesetzte Env-Variable "Db__ConnectionString" hat Vorrang (z. B. für CI).
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var environment =
            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString =
            config["Db:ConnectionString"]
            ?? throw new InvalidOperationException(
                "Db:ConnectionString wurde nicht gefunden. Führe den Befehl mit --startup-project " +
                $"Illig-AI-Platform aus, sodass dessen appsettings.json geladen wird (aktuell: {Directory.GetCurrentDirectory()}).");

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 40)));
        return new AppDbContext(optionsBuilder.Options);
    }
}
