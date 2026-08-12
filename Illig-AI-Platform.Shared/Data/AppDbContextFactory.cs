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

        var sharedProjectPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "Illig-AI-Platform.Shared");

        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            // Fallback: Falls das Startup-Projekt (z. B. Illig-AI-Platform) keinen Connection String
            // gesetzt hat (leerer String in appsettings.json), auf den in Illig-AI-Platform.Shared
            // hinterlegten Wert zurückfallen.
            .AddJsonFile(Path.Combine(sharedProjectPath, "appsettings.json"), optional: true)
            .AddJsonFile(Path.Combine(sharedProjectPath, $"appsettings.{environment}.json"), optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = config["Db:ConnectionString"];
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Db:ConnectionString ist leer oder wurde nicht gefunden. Führe den Befehl mit --startup-project " +
                $"Illig-AI-Platform aus, sodass dessen appsettings.json geladen wird (aktuell: {Directory.GetCurrentDirectory()}).");
        }

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 40)));
        return new AppDbContext(optionsBuilder.Options);
    }
}
