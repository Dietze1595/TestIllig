using System.Text;
using Illig_AI_Platform.Services.Auftragsinformationen;
using Illig_AI_Platform.Shared.Auftragsinformationen;
using Illig_AI_Platform.Shared.Data;
using Illig_AI_Platform.Shared.Kunden;
using Illig_AI_Platform.Shared.Plausibilitaetspruefung.Stuecklistenpruefung;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Illig_AI_Platform.Tests.Auftragsinformationen;

public class AuftragsinformationenImportServiceTests
{
    [Fact]
    public async Task Synchronisieren_speichert_nur_Referenz_und_Analyse_und_ist_idempotent()
    {
        await using var db = NeueDb();
        var graph = new FakeSharePointClient();
        var service = NeuerImport(db, graph);

        var ersterLauf = await service.SynchronisierenAsync();
        var zweiterLauf = await service.SynchronisierenAsync();

        Assert.Equal(1, ersterLauf.Verarbeitet);
        Assert.Equal(1, zweiterLauf.Uebersprungen);
        Assert.Equal(1, graph.OpenCount);

        var dokument = await db.StuecklistenpruefungVerlaufEintraege
            .SingleAsync(d => d.Quelle == AuftragsdokumentQuelle.SharePoint);
        Assert.Equal("drive-1", dokument.SharePointDriveId);
        Assert.Equal("item-1", dokument.SharePointItemId);
        Assert.Equal("11055627 / 40", dokument.Auftragsnummer);
        Assert.Equal(AuftragsdokumentAnalyseStatus.Erfolgreich, dokument.AnalyseStatus);
        Assert.Equal(2, await db.VerlaufMerkmale.CountAsync(m => m.VerlaufEintragId == dokument.Id));

        var stand = await db.SharePointSynchronisationsstaende.SingleAsync();
        Assert.Equal("delta-2", stand.DeltaLink);
    }

    [Fact]
    public async Task Geloeschtes_SharePoint_Dokument_wird_nicht_mehr_ausgeliefert()
    {
        await using var db = NeueDb();
        var graph = new FakeSharePointClient { DeleteOnSecondRun = true };
        var import = NeuerImport(db, graph);

        await import.SynchronisierenAsync();
        await import.SynchronisierenAsync();

        var dokument = await db.StuecklistenpruefungVerlaufEintraege
            .SingleAsync(d => d.Quelle == AuftragsdokumentQuelle.SharePoint);
        Assert.NotNull(dokument.GeloeschtAm);

        var dokumentService = new AuftragsdokumentService(db, graph);
        Assert.Empty(await dokumentService.ListeAsync());
        Assert.Null(await dokumentService.DokumentAsync(dokument.Id));
    }

    [Fact]
    public async Task Synchronisieren_legt_Kunde_an_und_registriert_Quelle_wie_DragAndDrop()
    {
        await using var db = NeueDb();
        var graph = new FakeSharePointClient();
        var service = NeuerImport(db, graph);

        await service.SynchronisierenAsync();

        var dokument = await db.StuecklistenpruefungVerlaufEintraege
            .SingleAsync(d => d.Quelle == AuftragsdokumentQuelle.SharePoint);

        // Kunde muss – genau wie beim Drag&Drop – im Kundenstamm angelegt und am Eintrag vermerkt sein.
        var kunde = Assert.Single(db.Kunden);
        Assert.Equal("708555", kunde.Kundennummer);
        Assert.Equal(kunde.Id, dokument.KundeId);

        // Und die Auftragsinformation muss als Kundenquelle registriert sein (per QuellId = Eintrag-Id).
        var quelle = Assert.Single(
            await db.KundenQuellen.Where(q => q.Quelltyp == KundenQuelltyp.Auftragsinformation).ToListAsync());
        Assert.Equal(dokument.Id, quelle.QuellId);
        Assert.Equal(kunde.Id, quelle.KundeId);
    }

    private static AuftragsinformationenImportService NeuerImport(
        AppDbContext db,
        FakeSharePointClient graph) =>
        new(
            db,
            graph,
            new FakeAnalyseService(),
            new KundenstammService(db),
            Options.Create(new SharePointAuftragsinformationenOptions
            {
                Enabled = true,
                HistoricalYears = 5,
            }),
            NullLogger<AuftragsinformationenImportService>.Instance);

    private static AppDbContext NeueDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private sealed class FakeSharePointClient : ISharePointDokumentClient
    {
        public int OpenCount { get; private set; }
        public bool DeleteOnSecondRun { get; init; }

        public Task<SharePointQuelle> QuelleAufloesenAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new SharePointQuelle("drive-1", "folder-1"));

        public Task<SharePointAenderungsseite> AenderungsseiteAsync(
            SharePointQuelle quelle,
            string? fortsetzungsUrl,
            CancellationToken cancellationToken = default)
        {
            if (fortsetzungsUrl is null)
            {
                var item = new SharePointAenderung(
                    quelle.DriveId, "item-1", "Auftragsinformation.pdf", "etag-1",
                    "https://example.test/document", DateTime.UtcNow.AddDays(-2), DateTime.UtcNow.AddDays(-1),
                    true, false);
                return Task.FromResult(new SharePointAenderungsseite([item], null, "delta-1"));
            }

            if (DeleteOnSecondRun)
            {
                var deleted = new SharePointAenderung(
                    quelle.DriveId, "item-1", "", "", "", DateTime.UnixEpoch, DateTime.UnixEpoch,
                    false, true);
                return Task.FromResult(new SharePointAenderungsseite([deleted], null, "delta-2"));
            }

            // Graph darf denselben Eintrag erneut liefern; ETag macht den Import idempotent.
            var unchanged = new SharePointAenderung(
                quelle.DriveId, "item-1", "Auftragsinformation.pdf", "etag-1",
                "https://example.test/document", DateTime.UtcNow.AddDays(-2), DateTime.UtcNow.AddDays(-1),
                true, false);
            return Task.FromResult(new SharePointAenderungsseite([unchanged], null, "delta-2"));
        }

        public Task<Stream> OeffnenAsync(
            string webUrl,
            CancellationToken cancellationToken = default)
        {
            OpenCount++;
            return Task.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes("pdf")));
        }
    }

    private sealed class FakeAnalyseService : IDocumentAnalyseService
    {
        public Task<DokumentAnalyseErgebnis> AnalyzeAsync(
            Stream pdfStream,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new DokumentAnalyseErgebnis(
                "11055627 / 40",
                "708555",
                new DateOnly(2026, 7, 1),
                "RDM 75Kc",
                [new ErkanntesMerkmal("40/20", "014936", "Merkmal")],
                [new ErkanntesMerkmal("40/330", "020182", "Sonderoption")],
                "Beispielkunde",
                "Musterstrasse 1"));
    }
}
