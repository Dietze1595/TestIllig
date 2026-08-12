using System.Security.Claims;
using Illig_AI_Platform.Controllers.Auftragsanlage;
using Illig_AI_Platform.Shared.Auftragsanlage;
using Illig_AI_Platform.Shared.Data;
using Illig_AI_Platform.Shared.Plausibilitaetspruefung.Stuecklistenpruefung;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Illig_AI_Platform.Tests.Controllers;

public class DashboardStatsTests
{
    [Fact]
    public async Task GetDashboardStats_ReturnsCorrectAggregation()
    {
        await using var db = NeueDb();
        var ownerId = Guid.NewGuid();

        // 1. Add some Angebote (Offers)
        db.Angebote.AddRange(
            new Angebot
            {
                Angebotsnummer = "11000001",
                Version = 1,
                HochgeladenAm = new DateTime(2026, 5, 10, 12, 0, 0, DateTimeKind.Utc),
                Dateiname = "angebot1.pdf",
                BlobPfad = "angebote/angebot1.pdf",
                Volltext = "Angebot 1"
            },
            new Angebot
            {
                Angebotsnummer = "11000002",
                Version = 1,
                HochgeladenAm = new DateTime(2026, 5, 20, 12, 0, 0, DateTimeKind.Utc),
                Dateiname = "angebot2.pdf",
                BlobPfad = "angebote/angebot2.pdf",
                Volltext = "Angebot 2"
            },
            new Angebot
            {
                Angebotsnummer = "11000003",
                Version = 1,
                // Different month
                HochgeladenAm = new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc),
                Dateiname = "angebot3.pdf",
                BlobPfad = "angebote/angebot3.pdf",
                Volltext = "Angebot 3"
            },
            new Angebot
            {
                Angebotsnummer = "11000004",
                Version = 1,
                // Different year
                HochgeladenAm = new DateTime(2025, 12, 1, 12, 0, 0, DateTimeKind.Utc),
                Dateiname = "angebot4.pdf",
                BlobPfad = "angebote/angebot4.pdf",
                Volltext = "Angebot 4"
            }
        );

        // 2. Add some Auftragsbestaetigungen (Orders)
        // Must associate them with some Angebot
        db.Auftragsbestaetigungen.AddRange(
            new Auftragsbestaetigung
            {
                Nummer = "B-2026-001",
                AngebotId = 1, // Will map to first Angebot
                HochgeladenAm = new DateTime(2026, 5, 25, 12, 0, 0, DateTimeKind.Utc),
                Dateiname = "bestellung1.pdf",
                BlobPfad = "bestellungen/bestellung1.pdf",
                Volltext = "Bestellung 1"
            },
            new Auftragsbestaetigung
            {
                Nummer = "B-2026-002",
                AngebotId = 3, // Will map to third Angebot
                HochgeladenAm = new DateTime(2026, 6, 20, 12, 0, 0, DateTimeKind.Utc),
                Dateiname = "bestellung2.pdf",
                BlobPfad = "bestellungen/bestellung2.pdf",
                Volltext = "Bestellung 2"
            }
        );

        await db.SaveChangesAsync();

        var controller = Controller(db, ownerId);

        // Query for year 2026
        var actionResult = await controller.GetDashboardStats(2026);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var stats = Assert.IsType<DashboardStatsAntwort>(okResult.Value);

        // Check years available
        Assert.Contains(2026, stats.Jahre);
        Assert.Contains(2025, stats.Jahre);

        // For year 2026:
        // May (month 5): 2 offers, 1 order
        // June (month 6): 1 offer, 1 order
        // Total for 2026: 3 offers, 2 orders
        Assert.Equal(3, stats.TotalAngebote);
        Assert.Equal(2, stats.TotalBestellungen);
        Assert.Equal(66.667, stats.Konversionsrate, 3); // 2 / 3 * 100

        var mayStat = stats.Monatswerte.FirstOrDefault(m => m.Monat == 5);
        Assert.NotNull(mayStat);
        Assert.Equal(2, mayStat.AngeboteCount);
        Assert.Equal(1, mayStat.BestellungenCount);

        var juneStat = stats.Monatswerte.FirstOrDefault(m => m.Monat == 6);
        Assert.NotNull(juneStat);
        Assert.Equal(1, juneStat.AngeboteCount);
        Assert.Equal(1, juneStat.BestellungenCount);

        // Other months should be 0
        var janStat = stats.Monatswerte.FirstOrDefault(m => m.Monat == 1);
        Assert.NotNull(janStat);
        Assert.Equal(0, janStat.AngeboteCount);
        Assert.Equal(0, janStat.BestellungenCount);
    }

    [Fact]
    public async Task GetDashboardStats_ForAllYears_ReturnsCorrectOverallAggregation()
    {
        await using var db = NeueDb();
        var ownerId = Guid.NewGuid();

        db.Angebote.AddRange(
            new Angebot { Angebotsnummer = "A1", Version = 1, HochgeladenAm = new DateTime(2026, 5, 10, 0, 0, 0, DateTimeKind.Utc), Dateiname = "a1.pdf", BlobPfad = "b1", Volltext = "A1" },
            new Angebot { Angebotsnummer = "A2", Version = 1, HochgeladenAm = new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc), Dateiname = "a2.pdf", BlobPfad = "b2", Volltext = "A2" }
        );

        db.Auftragsbestaetigungen.Add(
            new Auftragsbestaetigung { Nummer = "B1", AngebotId = 1, HochgeladenAm = new DateTime(2026, 5, 25, 0, 0, 0, DateTimeKind.Utc), Dateiname = "b1.pdf", BlobPfad = "b3", Volltext = "B1" }
        );

        await db.SaveChangesAsync();

        var controller = Controller(db, ownerId);

        // Query for all years (year = 0)
        var actionResult = await controller.GetDashboardStats(0);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var stats = Assert.IsType<DashboardStatsAntwort>(okResult.Value);

        Assert.Equal(2, stats.TotalAngebote);
        Assert.Equal(1, stats.TotalBestellungen);
        Assert.Equal(50.0, stats.Konversionsrate);

        // Check years values list
        var stat2026 = stats.Jahreswerte.FirstOrDefault(j => j.Jahr == 2026);
        Assert.NotNull(stat2026);
        Assert.Equal(1, stat2026.AngeboteCount);
        Assert.Equal(1, stat2026.BestellungenCount);

        var stat2025 = stats.Jahreswerte.FirstOrDefault(j => j.Jahr == 2025);
        Assert.NotNull(stat2025);
        Assert.Equal(1, stat2025.AngeboteCount);
        Assert.Equal(0, stat2025.BestellungenCount);
    }

    private static AppDbContext NeueDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static AuftragsanlageController Controller(AppDbContext db, Guid benutzerId)
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("oid", benutzerId.ToString())], "test")),
        };

        return new AuftragsanlageController(
            new FakeAnalyseService(),
            new AngebotsService(db, new FakeBlobStorageService()),
            new FakeVergleichService(),
            new NichtKonfigurierterVersandartLlmService(),
            NullLogger<AuftragsanlageController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
        };
    }

    private sealed class FakeAnalyseService : IAngebotsdokumentAnalyseService
    {
        public Task<ExtrahierteAngebotsdaten> AnalyzeAsync(
            Stream pdfStream, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class FakeVergleichService : IAngebotsvergleichLlmService
    {
        public Task<AngebotsVergleichLlmErgebnis> VergleicheAsync(
            string angebotVolltext, string bestaetigungVolltext,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class FakeBlobStorageService : IBlobStorageService
    {
        public Task<string> UploadAsync(
            Stream inhalt, string dateiname, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Stream> OpenReadAsync(
            string blobPfad, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task DeleteAsync(
            string blobPfad, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
