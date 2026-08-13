using Illig_AI_Platform.Shared.Data;
using Illig_AI_Platform.Shared.Plausibilitaetspruefung;
using Illig_AI_Platform.Shared.Plausibilitaetspruefung.Sondermerkmalsuche;
using Illig_AI_Platform.Shared.Plausibilitaetspruefung.Stuecklistenpruefung;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Illig_AI_Platform.Tests.Plausibilitaetspruefung.Sondermerkmalsuche;

public class DatenbankSondermerkmalServiceTests
{
    private sealed class FakeBlobStorageService : IBlobStorageService
    {
        public string? GeoeffneterBlobPfad { get; private set; }

        public Task<string> UploadAsync(
            Stream inhalt,
            string dateiname,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Stream> OpenReadAsync(
            string blobPfad,
            CancellationToken cancellationToken = default)
        {
            GeoeffneterBlobPfad = blobPfad;
            return Task.FromResult<Stream>(new MemoryStream("pdf"u8.ToArray()));
        }

        public Task DeleteAsync(string blobPfad, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class FakeSharePointDokumentClient : Illig_AI_Platform.Services.Auftragsinformationen.ISharePointDokumentClient
    {
        public string? GeoeffneteDriveId { get; private set; }
        public string? GeoeffneteItemId { get; private set; }

        public Task<Illig_AI_Platform.Services.Auftragsinformationen.SharePointQuelle> QuelleAufloesenAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Illig_AI_Platform.Services.Auftragsinformationen.SharePointAenderungsseite> AenderungsseiteAsync(
            Illig_AI_Platform.Services.Auftragsinformationen.SharePointQuelle quelle,
            string? fortsetzungsUrl,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Stream> OeffnenAsync(
            string driveId,
            string itemId,
            CancellationToken cancellationToken = default)
        {
            GeoeffneteDriveId = driveId;
            GeoeffneteItemId = itemId;
            return Task.FromResult<Stream>(new MemoryStream("sharepoint-pdf"u8.ToArray()));
        }
    }

    private static AppDbContext NeueDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static async Task SeedEintragAsync(
        AppDbContext db, string auftragsnummer, string maschinentyp, DateOnly? datum, DateTime erstelltAm,
        string dateiname, params (string Position, string Nummer, string Beschreibung)[] sonderoptionen)
    {
        var eintrag = new StuecklistenpruefungVerlaufEintrag
        {
            UserProfileId = Guid.NewGuid(),
            Dateiname = dateiname,
            BlobPfad = $"blob/{dateiname}",
            Auftragsnummer = auftragsnummer,
            Kundennummer = "708555",
            Datum = datum,
            Maschinentyp = maschinentyp,
            ErstelltAm = erstelltAm,
        };
        db.StuecklistenpruefungVerlaufEintraege.Add(eintrag);
        await db.SaveChangesAsync();

        db.VerlaufMerkmale.AddRange(sonderoptionen.Select(s => new VerlaufMerkmal
        {
            VerlaufEintragId = eintrag.Id,
            Kategorie = MerkmalKategorie.Sonderoption,
            Position = s.Position,
            Merkmalsnummer = s.Nummer,
            Beschreibung = s.Beschreibung,
        }));
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task AnalyzeAsync_FindetAuftrag_WennAuftragsnummerMitPositionssuffixGespeichertIst()
    {
        var db = NeueDb();
        await SeedEintragAsync(db, "11055627 / 40", "RDK 80k", new DateOnly(2025, 5, 21), new DateTime(2026, 1, 1),
            "a.pdf", ("30", "SM-1001", "Beschreibung"));
        var service = new DatenbankSondermerkmalService(db);

        var result = await service.AnalyzeAsync("11055627");

        Assert.NotNull(result);
        Assert.Equal("11055627", result!.Auftragsnummer);
        var merkmal = Assert.Single(result.Sondermerkmale);
        Assert.Equal("SM-1001", merkmal.Merkmalsnummer);
    }

    [Fact]
    public async Task AnalyzeAsync_OhneLinie_VereinigtSonderoptionenAllerLinien()
    {
        var db = NeueDb();
        await SeedEintragAsync(db, "11055627 / 40", "RDK 80k", new DateOnly(2025, 5, 21), new DateTime(2026, 1, 1),
            "linie40.pdf", ("30", "SM-40", "Linie 40"));
        await SeedEintragAsync(db, "11055627 / 50", "RDK 80k", new DateOnly(2025, 5, 21), new DateTime(2026, 1, 2),
            "linie50.pdf", ("30", "SM-50", "Linie 50"));
        var service = new DatenbankSondermerkmalService(db);

        var result = await service.AnalyzeAsync("11055627");

        Assert.NotNull(result);
        Assert.Equal("11055627", result!.Auftragsnummer);
        Assert.Equal(2, result.Sondermerkmale.Count);
        Assert.Contains(result.Sondermerkmale, m => m.Merkmalsnummer == "SM-40");
        Assert.Contains(result.Sondermerkmale, m => m.Merkmalsnummer == "SM-50");
    }

    [Fact]
    public async Task AnalyzeAsync_MitLinie_LiefertNurSonderoptionenDieserLinie()
    {
        var db = NeueDb();
        await SeedEintragAsync(db, "11055627 / 40", "RDK 80k", new DateOnly(2025, 5, 21), new DateTime(2026, 1, 1),
            "linie40.pdf", ("30", "SM-40", "Linie 40"));
        await SeedEintragAsync(db, "11055627 / 50", "RDK 80k", new DateOnly(2025, 5, 21), new DateTime(2026, 1, 2),
            "linie50.pdf", ("30", "SM-50", "Linie 50"));
        var service = new DatenbankSondermerkmalService(db);

        var result = await service.AnalyzeAsync("11055627 / 40");

        Assert.NotNull(result);
        Assert.Equal("11055627 / 40", result!.Auftragsnummer);
        var merkmal = Assert.Single(result.Sondermerkmale);
        Assert.Equal("SM-40", merkmal.Merkmalsnummer);
    }

    [Fact]
    public async Task AnalyzeAsync_MitLinie_AkzeptiertEingabeOhneLeerzeichenUmDenSchraegstrich()
    {
        var db = NeueDb();
        await SeedEintragAsync(db, "11055627 / 40", "RDK 80k", new DateOnly(2025, 5, 21), new DateTime(2026, 1, 1),
            "linie40.pdf", ("30", "SM-40", "Linie 40"));
        await SeedEintragAsync(db, "11055627 / 50", "RDK 80k", new DateOnly(2025, 5, 21), new DateTime(2026, 1, 2),
            "linie50.pdf", ("30", "SM-50", "Linie 50"));
        var service = new DatenbankSondermerkmalService(db);

        var result = await service.AnalyzeAsync("11055627/40");

        Assert.NotNull(result);
        var merkmal = Assert.Single(result!.Sondermerkmale);
        Assert.Equal("SM-40", merkmal.Merkmalsnummer);
    }

    [Fact]
    public async Task AnalyzeAsync_MitLinie_LiefertNull_WennLinieNichtExistiert()
    {
        var db = NeueDb();
        await SeedEintragAsync(db, "11055627 / 40", "RDK 80k", new DateOnly(2025, 5, 21), new DateTime(2026, 1, 1),
            "linie40.pdf", ("30", "SM-40", "Linie 40"));
        var service = new DatenbankSondermerkmalService(db);

        var result = await service.AnalyzeAsync("11055627 / 99");

        Assert.Null(result);
    }

    [Fact]
    public async Task AnalyzeAsync_VerwechseltAehnlicheAuftragsnummernNicht()
    {
        var db = NeueDb();
        await SeedEintragAsync(db, "1105562 / 10", "RDK 80k", new DateOnly(2025, 5, 21), new DateTime(2026, 1, 1),
            "kurz.pdf", ("30", "SM-KURZ", "Beschreibung"));
        await SeedEintragAsync(db, "11055627 / 40", "RDK 80k", new DateOnly(2025, 5, 21), new DateTime(2026, 1, 1),
            "lang.pdf", ("30", "SM-LANG", "Beschreibung"));
        var service = new DatenbankSondermerkmalService(db);

        var result = await service.AnalyzeAsync("1105562");

        Assert.NotNull(result);
        var merkmal = Assert.Single(result!.Sondermerkmale);
        Assert.Equal("SM-KURZ", merkmal.Merkmalsnummer);
    }

    [Fact]
    public async Task SearchAsync_SchliesstQuellauftragAus_TrotzPositionssuffixInGespeicherterAuftragsnummer()
    {
        var db = NeueDb();
        await SeedEintragAsync(db, "11055627 / 40", "RDK 80k", new DateOnly(2025, 5, 21), new DateTime(2026, 1, 1),
            "quelle.pdf", ("30", "SM-1001", "A"));
        await SeedEintragAsync(db, "4500011004", "RDK 80k", new DateOnly(2025, 4, 1), new DateTime(2025, 12, 1),
            "treffer.pdf", ("30", "SM-1001", "A"));
        var service = new DatenbankSondermerkmalService(db);

        var result = await service.SearchAsync(new SearchRequest("11055627", ["SM-1001"]));

        Assert.DoesNotContain(result, t => t.Auftragsnummer == "11055627" || t.Auftragsnummer == "11055627 / 40");
        Assert.Contains(result, t => t.Auftragsnummer == "4500011004");
    }

    [Fact]
    public async Task AnalyzeAsync_LiefertKundennameUndKundennummer()
    {
        var db = NeueDb();
        db.StuecklistenpruefungVerlaufEintraege.Add(new StuecklistenpruefungVerlaufEintrag
        {
            UserProfileId = Guid.NewGuid(),
            Dateiname = "a.pdf",
            BlobPfad = "blob/a.pdf",
            Auftragsnummer = "4500012345",
            Kundennummer = "712082",
            Kundenname = "PLASZOM, Orleans - SC",
            Datum = new DateOnly(2025, 12, 2),
            Maschinentyp = "RS 91",
            ErstelltAm = new DateTime(2026, 1, 1),
        });
        await db.SaveChangesAsync();
        var service = new DatenbankSondermerkmalService(db);

        var result = await service.AnalyzeAsync("4500012345");

        Assert.NotNull(result);
        Assert.Equal("712082", result!.Kundennummer);
        Assert.Equal("PLASZOM, Orleans - SC", result.Kundenname);
    }

    [Fact]
    public async Task SearchAsync_LiefertKundennameUndKundennummerJeTreffer()
    {
        var db = NeueDb();
        var eintrag = new StuecklistenpruefungVerlaufEintrag
        {
            UserProfileId = Guid.NewGuid(),
            Dateiname = "treffer.pdf",
            BlobPfad = "blob/treffer.pdf",
            Auftragsnummer = "4500011004",
            Kundennummer = "712082",
            Kundenname = "PLASZOM, Orleans - SC",
            Datum = new DateOnly(2025, 4, 1),
            Maschinentyp = "RDK 80k",
            ErstelltAm = new DateTime(2025, 12, 1),
        };
        db.StuecklistenpruefungVerlaufEintraege.Add(eintrag);
        await db.SaveChangesAsync();
        db.VerlaufMerkmale.Add(new VerlaufMerkmal
        {
            VerlaufEintragId = eintrag.Id,
            Kategorie = MerkmalKategorie.Sonderoption,
            Position = "30",
            Merkmalsnummer = "SM-1001",
            Beschreibung = "A",
        });
        await db.SaveChangesAsync();
        var service = new DatenbankSondermerkmalService(db);

        var result = await service.SearchAsync(new SearchRequest(null, ["SM-1001"]));

        var treffer = Assert.Single(result);
        Assert.Equal("712082", treffer.Kundennummer);
        Assert.Equal("PLASZOM, Orleans - SC", treffer.Kundenname);
    }

    [Fact]
    public async Task AnalyzeAsync_LiefertNull_WennKeinVerlaufEintrag()
    {
        var db = NeueDb();
        var service = new DatenbankSondermerkmalService(db);

        var result = await service.AnalyzeAsync("9999999999");

        Assert.Null(result);
    }

    [Fact]
    public async Task AnalyzeAsync_LiefertSonderoptionenEinesEintrags()
    {
        var db = NeueDb();
        await SeedEintragAsync(db, "4500012345", "RDK 80k", new DateOnly(2025, 5, 21), new DateTime(2026, 1, 1),
            "a.pdf", ("30", "SM-1001", "Erhöhte Reinraumtauglichkeit"));
        var service = new DatenbankSondermerkmalService(db);

        var result = await service.AnalyzeAsync("4500012345");

        Assert.NotNull(result);
        Assert.Equal("4500012345", result!.Auftragsnummer);
        Assert.Equal("RDK 80k", result.Maschinentyp);
        Assert.Equal("708555", result.Kundennummer);
        var merkmal = Assert.Single(result.Sondermerkmale);
        Assert.Equal("30", merkmal.Position);
        Assert.Equal("SM-1001", merkmal.Merkmalsnummer);
    }

    [Fact]
    public async Task AnalyzeAsync_VereinigtSonderoptionenMehrererEintraege_NeuesterEintragGewinntBeiKonflikt()
    {
        var db = NeueDb();
        await SeedEintragAsync(db, "4500012345", "RDK 80k", new DateOnly(2025, 5, 21), new DateTime(2026, 1, 1),
            "erster-upload.pdf", ("30", "SM-1001", "Alte Beschreibung"));
        await SeedEintragAsync(db, "4500012345", "RDK 80k", new DateOnly(2025, 5, 21), new DateTime(2026, 1, 5),
            "zweiter-upload.pdf", ("30", "SM-1001", "Neue Beschreibung"), ("45", "SM-1010", "Zusatzkühlung"));
        var service = new DatenbankSondermerkmalService(db);

        var result = await service.AnalyzeAsync("4500012345");

        Assert.NotNull(result);
        Assert.Equal(2, result!.Sondermerkmale.Count);
        Assert.Contains(result.Sondermerkmale, m => m.Merkmalsnummer == "SM-1001" && m.Beschreibung == "Neue Beschreibung");
        Assert.Contains(result.Sondermerkmale, m => m.Merkmalsnummer == "SM-1010");
    }

    [Fact]
    public async Task AnalyzeAsync_FaelltAufErstelltAmZurueck_WennDatumFehlt()
    {
        var db = NeueDb();
        await SeedEintragAsync(db, "4500012345", "RDK 80k", null, new DateTime(2026, 2, 10),
            "a.pdf", ("30", "SM-1001", "Beschreibung"));
        var service = new DatenbankSondermerkmalService(db);

        var result = await service.AnalyzeAsync("4500012345");

        Assert.NotNull(result);
        Assert.Equal(new DateOnly(2026, 2, 10), result!.Datum);
    }

    [Fact]
    public async Task AnalyzeAsync_GibtPositionUnveraendertAlsStringZurueck()
    {
        var db = NeueDb();
        await SeedEintragAsync(db, "4500012345", "RDK 80k", new DateOnly(2025, 5, 21), new DateTime(2026, 1, 1),
            "a.pdf", ("Zusatz", "SM-1001", "Beschreibung"));
        var service = new DatenbankSondermerkmalService(db);

        var result = await service.AnalyzeAsync("4500012345");

        var merkmal = Assert.Single(result!.Sondermerkmale);
        Assert.Equal("Zusatz", merkmal.Position);
    }

    [Fact]
    public async Task AnalyzeAsync_BehaeltGruppenUndUnterpositionenAlsUnterscheidbareStrings()
    {
        var db = NeueDb();
        await SeedEintragAsync(db, "4500012345", "RDK 80k", new DateOnly(2025, 5, 21), new DateTime(2026, 1, 1),
            "a.pdf", ("40/330", "SM-1001", "Schaltschrank"), ("40/340", "SM-1002", "Schnittstelle"));
        var service = new DatenbankSondermerkmalService(db);

        var result = await service.AnalyzeAsync("4500012345");

        Assert.NotNull(result);
        Assert.Equal(2, result!.Sondermerkmale.Count);
        Assert.Contains(result.Sondermerkmale, m => m.Position == "40/330" && m.Merkmalsnummer == "SM-1001");
        Assert.Contains(result.Sondermerkmale, m => m.Position == "40/340" && m.Merkmalsnummer == "SM-1002");
    }

    [Fact]
    public async Task SearchAsync_SchliesstQuellauftragAus_UndRanktNachTrefferzahl()
    {
        var db = NeueDb();
        await SeedEintragAsync(db, "4500012345", "RDK 80k", new DateOnly(2025, 5, 21), new DateTime(2026, 1, 1),
            "quelle.pdf", ("30", "SM-1001", "A"), ("45", "SM-1010", "B"));
        await SeedEintragAsync(db, "4500011004", "RDK 80k", new DateOnly(2025, 4, 1), new DateTime(2025, 12, 1),
            "voller-treffer.pdf", ("30", "SM-1001", "A"), ("45", "SM-1010", "B"));
        await SeedEintragAsync(db, "4500011005", "RDK 80k", new DateOnly(2025, 3, 1), new DateTime(2025, 11, 1),
            "teiltreffer.pdf", ("30", "SM-1001", "A"));
        var service = new DatenbankSondermerkmalService(db);

        var result = await service.SearchAsync(new SearchRequest("4500012345", ["SM-1001", "SM-1010"]));

        Assert.DoesNotContain(result, t => t.Auftragsnummer == "4500012345");
        Assert.Equal("4500011004", result[0].Auftragsnummer);
        Assert.Equal(2, result[0].TrefferAnzahl);
        Assert.Equal("4500011005", result[1].Auftragsnummer);
        Assert.Equal(1, result[1].TrefferAnzahl);
    }

    [Fact]
    public async Task SearchAsync_MarkiertAktuellstesVorkommenJeMerkmal_GlobalVorTop5()
    {
        var db = NeueDb();
        await SeedEintragAsync(db, "QUELLE-1", "RDK 80k", new DateOnly(2026, 1, 1), new DateTime(2026, 1, 1),
            "quelle.pdf", ("30", "SM-1001", "A"), ("45", "SM-1010", "B"));
        await SeedEintragAsync(db, "ALT-VOLL", "RDK 80k", new DateOnly(2025, 1, 1), new DateTime(2025, 1, 1),
            "alt.pdf", ("30", "SM-1001", "A"), ("45", "SM-1010", "B"));
        await SeedEintragAsync(db, "NEU-M1", "RDK 80k", new DateOnly(2025, 6, 1), new DateTime(2025, 6, 1),
            "neu-m1.pdf", ("30", "SM-1001", "A"));
        await SeedEintragAsync(db, "NEU-M2", "RDK 80k", new DateOnly(2025, 7, 1), new DateTime(2025, 7, 1),
            "neu-m2.pdf", ("45", "SM-1010", "B"));
        var service = new DatenbankSondermerkmalService(db);

        var result = await service.SearchAsync(new SearchRequest("QUELLE-1", ["SM-1001", "SM-1010"]));

        Assert.Empty(result.Single(t => t.Auftragsnummer == "ALT-VOLL").AktuellsteMerkmalsnummern);
        Assert.Equal(
            ["SM-1001"],
            result.Single(t => t.Auftragsnummer == "NEU-M1").AktuellsteMerkmalsnummern);
        Assert.Equal(
            ["SM-1010"],
            result.Single(t => t.Auftragsnummer == "NEU-M2").AktuellsteMerkmalsnummern);
    }

    [Fact]
    public async Task GetDetailAsync_LiefertNull_WennKeinVerlaufEintrag()
    {
        var db = NeueDb();
        var service = new DatenbankSondermerkmalService(db);

        var result = await service.GetDetailAsync("9999999999");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetDetailAsync_ZeigtQuelleDesUploadsProMerkmal()
    {
        var db = NeueDb();
        await SeedEintragAsync(db, "4500012345", "RDK 80k", new DateOnly(2025, 5, 21), new DateTime(2026, 1, 1),
            "erster-upload.pdf", ("30", "SM-1001", "A"));
        await SeedEintragAsync(db, "4500012345", "RDK 80k", new DateOnly(2025, 5, 21), new DateTime(2026, 1, 5),
            "zweiter-upload.pdf", ("45", "SM-1010", "B"));
        var service = new DatenbankSondermerkmalService(db);

        var result = await service.GetDetailAsync("4500012345");

        Assert.NotNull(result);
        Assert.Equal(2, result!.Merkmale.Count);
        Assert.Contains(result.Merkmale, m => m.Merkmalsnummer == "SM-1001" && m.Quelle == "erster-upload.pdf");
        Assert.Contains(result.Merkmale, m => m.Merkmalsnummer == "SM-1010" && m.Quelle == "zweiter-upload.pdf");
        Assert.All(result.Merkmale, m => Assert.Equal("4500012345", m.Referenzauftrag));
    }

    [Fact]
    public async Task GetDokumentAsync_LiefertNeuestenUploadDesReferenzauftrags()
    {
        var db = NeueDb();
        await SeedEintragAsync(db, "4500012345 / 10", "RDK 80k", new DateOnly(2025, 5, 21), new DateTime(2026, 1, 1),
            "erster-upload.pdf", ("30", "SM-1001", "A"));
        await SeedEintragAsync(db, "4500012345 / 20", "RDK 80k", new DateOnly(2025, 5, 21), new DateTime(2026, 1, 5),
            "zweiter-upload.pdf", ("45", "SM-1010", "B"));
        var blobStorage = new FakeBlobStorageService();
        var service = new DatenbankSondermerkmalService(db, blobStorage);

        var result = await service.GetDokumentAsync("4500012345");

        Assert.NotNull(result);
        Assert.Equal("zweiter-upload.pdf", result!.Dateiname);
        Assert.Equal("blob/zweiter-upload.pdf", blobStorage.GeoeffneterBlobPfad);
    }

    [Fact]
    public async Task GetDokumentAsync_LiefertSharePointDokument_WennQuelleSharePointIst()
    {
        var db = NeueDb();
        var eintrag = new StuecklistenpruefungVerlaufEintrag
        {
            UserProfileId = Guid.NewGuid(),
            Quelle = AuftragsdokumentQuelle.SharePoint,
            Dateiname = "sharepoint-doc.pdf",
            SharePointDriveId = "drive-id-123",
            SharePointItemId = "item-id-456",
            Auftragsnummer = "4500012345",
            Kundennummer = "708555",
            Datum = new DateOnly(2025, 5, 21),
            Maschinentyp = "RDK 80k",
            ErstelltAm = new DateTime(2026, 1, 5),
        };
        db.StuecklistenpruefungVerlaufEintraege.Add(eintrag);
        await db.SaveChangesAsync();

        var sharePointClient = new FakeSharePointDokumentClient();
        var service = new DatenbankSondermerkmalService(db, sharePointClient: sharePointClient);

        var result = await service.GetDokumentAsync("4500012345");

        Assert.NotNull(result);
        Assert.Equal("sharepoint-doc.pdf", result!.Dateiname);
        Assert.Equal("drive-id-123", sharePointClient.GeoeffneteDriveId);
        Assert.Equal("item-id-456", sharePointClient.GeoeffneteItemId);
        using var reader = new StreamReader(result.Inhalt);
        var content = await reader.ReadToEndAsync();
        Assert.Equal("sharepoint-pdf", content);
    }

    [Fact]
    public async Task SearchAsync_ZeigtAlleTreffer_WennKeineQuellauftragsnummerGesetzt()
    {
        var db = NeueDb();
        for (var i = 1; i <= 6; i++)
        {
            await SeedEintragAsync(db, $"AUFTRAG-{i}", "RDK 80k", new DateOnly(2025, 1, i), new DateTime(2025, 1, i),
                $"treffer{i}.pdf", ("30", "SM-1001", "A"));
        }
        var service = new DatenbankSondermerkmalService(db);

        var result = await service.SearchAsync(new SearchRequest(null, ["SM-1001"]));

        Assert.Equal(6, result.Count);
    }

    [Fact]
    public async Task SearchAsync_BegrenztWeiterhinAufTop5_WennQuellauftragsnummerGesetzt()
    {
        var db = NeueDb();
        await SeedEintragAsync(db, "QUELLE-1", "RDK 80k", new DateOnly(2025, 5, 21), new DateTime(2026, 1, 1),
            "quelle.pdf", ("30", "SM-1001", "A"));
        for (var i = 1; i <= 6; i++)
        {
            await SeedEintragAsync(db, $"AUFTRAG-{i}", "RDK 80k", new DateOnly(2025, 1, i), new DateTime(2025, 1, i),
                $"treffer{i}.pdf", ("30", "SM-1001", "A"));
        }
        var service = new DatenbankSondermerkmalService(db);

        var result = await service.SearchAsync(new SearchRequest("QUELLE-1", ["SM-1001"]));

        Assert.Equal(5, result.Count);
    }
}
