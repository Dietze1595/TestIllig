using Illig_AI_Platform.Shared.Data;
using Illig_AI_Platform.Shared.Plausibilitaetspruefung.Stuecklistenpruefung;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Illig_AI_Platform.Tests.Plausibilitaetspruefung.Stuecklistenpruefung;

public class StuecklistenImportServiceTests
{
    // StuecklistenImportService umschließt den Full-Replace-Import mit einer Transaktion (echtes
    // Verhalten gegen MySQL/Pomelo). Der InMemory-Provider unterstützt keine Transaktionen und
    // würde ohne diese Warnungsunterdrückung mit einer TransactionIgnoredWarning abbrechen — die
    // Transaktion wird dort einfach als No-Op behandelt.
    private static AppDbContext CreateDb() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
        .Options);

    private static RohPosition Baum() => new("9209307", "RDM 75Kc", 0m, "", [
        new RohPosition("9237787", "Folieneinlauf_FB_200-900_konf", 1m, "ST", [
            new RohPosition("9237831", "Lichtleiter_BGR", 1m, "ST", [])
        ])
    ]);

    private sealed class SammelndeLogger : ILogger<StuecklistenImportService>
    {
        public List<string> Warnungen { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Warning)
                Warnungen.Add(formatter(state, exception));
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }
    }

    [Fact]
    public async Task ImportierenAsync_SpeichertBaumMitBedingung()
    {
        await using var db = CreateDb();
        var service = new StuecklistenImportService(db, NullLogger<StuecklistenImportService>.Instance);
        var matrixZeilen = new List<MatrixZeile>
        {
            new(["9209307", "9237787", "9237831"], "020113 / 020114")
        };

        await service.ImportierenAsync("RDM 75Kc", "9209307", Baum(), matrixZeilen);

        var positionen = await db.MaximalstuecklistenPositionen.ToListAsync();
        Assert.Equal(3, positionen.Count);
        var lichtleiter = Assert.Single(positionen, p => p.Artikelnummer == "9237831");
        Assert.Equal("020113 / 020114", lichtleiter.Bedingung);
        var wurzel = Assert.Single(positionen, p => p.Artikelnummer == "9209307");
        Assert.Null(wurzel.Bedingung);
    }

    [Fact]
    public async Task ImportierenAsync_ErsetztVorherigenImportKomplett()
    {
        await using var db = CreateDb();
        var service = new StuecklistenImportService(db, NullLogger<StuecklistenImportService>.Instance);

        await service.ImportierenAsync("RDM 75Kc", "9209307", Baum(), []);
        await service.ImportierenAsync("RDM 75Kc", "9209307", Baum(), []);

        Assert.Equal(1, await db.MaschinentypStuecklisten.CountAsync());
        Assert.Equal(3, await db.MaximalstuecklistenPositionen.CountAsync());
    }

    [Fact]
    public async Task ImportierenAsync_ProtokolliertNichtZuordenbareMatrixZeile_OhneImportAbzubrechen()
    {
        await using var db = CreateDb();
        var logger = new SammelndeLogger();
        var service = new StuecklistenImportService(db, logger);
        var matrixZeilen = new List<MatrixZeile>
        {
            new(["9209307", "9237787", "9237831"], "020113 / 020114"),
            new(["9209307", "9999999"], "999999") // existiert nirgends im Baum
        };

        await service.ImportierenAsync("RDM 75Kc", "9209307", Baum(), matrixZeilen);

        Assert.Equal(3, await db.MaximalstuecklistenPositionen.CountAsync());
        Assert.Single(logger.Warnungen, w => w.Contains("9999999"));
    }
}
