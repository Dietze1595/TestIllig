using Illig_AI_Platform.Shared.Data;
using Illig_AI_Platform.Shared.Kunden;
using Illig_AI_Platform.Shared.Plausibilitaetspruefung.Stuecklistenpruefung;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Illig_AI_Platform.Tests.Plausibilitaetspruefung.Stuecklistenpruefung;

public class StuecklistenpruefungVerlaufServiceTests
{
    private class FakeBlobStorageService : IBlobStorageService
    {
        public List<string> HochgeladeneDateinamen { get; } = [];
        public List<string> GeloeschteBlobPfade { get; } = [];

        public Task<string> UploadAsync(Stream inhalt, string dateiname, CancellationToken cancellationToken = default)
        {
            HochgeladeneDateinamen.Add(dateiname);
            return Task.FromResult($"blob/{dateiname}");
        }

        public Task<Stream> OpenReadAsync(string blobPfad, CancellationToken cancellationToken = default) =>
            Task.FromResult<Stream>(new MemoryStream([1, 2, 3]));

        public Task DeleteAsync(string blobPfad, CancellationToken cancellationToken = default)
        {
            GeloeschteBlobPfade.Add(blobPfad);
            return Task.CompletedTask;
        }
    }

    private class FakeSharePointDokumentClient : Illig_AI_Platform.Services.Auftragsinformationen.ISharePointDokumentClient
    {
        public string? GeoeffneteWebUrl { get; private set; }

        public Task<Illig_AI_Platform.Services.Auftragsinformationen.SharePointQuelle> QuelleAufloesenAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Illig_AI_Platform.Services.Auftragsinformationen.SharePointAenderungsseite> AenderungsseiteAsync(
            Illig_AI_Platform.Services.Auftragsinformationen.SharePointQuelle quelle,
            string? fortsetzungsUrl,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Stream> OeffnenAsync(
            string webUrl,
            CancellationToken cancellationToken = default)
        {
            GeoeffneteWebUrl = webUrl;
            return Task.FromResult<Stream>(new MemoryStream([7, 8, 9]));
        }
    }

    private static AppDbContext NeueDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static DokumentAnalyseErgebnis GueltigesErgebnis() => new(
        "11055627 / 40", "708555", new DateOnly(2025, 5, 21), "RDK 80k",
        [new ErkanntesMerkmal("40", "9209425", "Beschreibung")], []);

    private const string PlaszomAdresse = "PO Box 06 ORLEANS - SC 88870-000 BRASILIEN";

    private static DokumentAnalyseErgebnis PlaszomErgebnis() => new(
        "11055783 / 60", "712082", new DateOnly(2025, 12, 2), "RS 91_konf",
        [new ErkanntesMerkmal("40", "9209425", "Beschreibung")], [],
        Kundenname: "PLASZOM, Orleans - SC",
        Kundenadresse: PlaszomAdresse);

    [Fact]
    public async Task SpeichernAsync_LegtKundeMitNameUndAdresseAn_UndVermerktSieAmEintrag()
    {
        var db = NeueDb();
        var service = new StuecklistenpruefungVerlaufService(db, new FakeBlobStorageService(), new KundenstammService(db));

        var id = await service.SpeichernAsync(Guid.NewGuid(), "plaszom.pdf", new MemoryStream([1]), PlaszomErgebnis());

        var kunde = Assert.Single(db.Kunden);
        Assert.Equal("712082", kunde.Kundennummer);
        Assert.Equal("PLASZOM, Orleans - SC", kunde.Name);
        Assert.Equal(PlaszomAdresse, kunde.Adresse);

        var eintrag = await db.StuecklistenpruefungVerlaufEintraege.SingleAsync(e => e.Id == id);
        Assert.Equal(kunde.Id, eintrag.KundeId);
        Assert.Equal("PLASZOM, Orleans - SC", eintrag.Kundenname);
        Assert.Equal(PlaszomAdresse, eintrag.Kundenadresse);
    }

    [Fact]
    public async Task SpeichernAsync_GleicheKundennummer_LegtKeinenZweitenKundenAn()
    {
        var db = NeueDb();
        var kundenstamm = new KundenstammService(db);
        var service = new StuecklistenpruefungVerlaufService(db, new FakeBlobStorageService(), kundenstamm);

        await service.SpeichernAsync(Guid.NewGuid(), "a.pdf", new MemoryStream([1]), PlaszomErgebnis());
        await service.SpeichernAsync(Guid.NewGuid(), "b.pdf", new MemoryStream([2]), PlaszomErgebnis());

        Assert.Equal(1, await db.Kunden.CountAsync());
        Assert.Equal(2, await db.StuecklistenpruefungVerlaufEintraege.CountAsync());
        Assert.All(await db.StuecklistenpruefungVerlaufEintraege.ToListAsync(),
            e => Assert.Equal(db.Kunden.Single().Id, e.KundeId));
    }

    [Fact]
    public async Task SpeichernAsync_LegtEintragFuerUserAn()
    {
        var db = NeueDb();
        var blob = new FakeBlobStorageService();
        var service = new StuecklistenpruefungVerlaufService(db, blob);
        var userId = Guid.NewGuid();

        var id = await service.SpeichernAsync(userId, "test.pdf", new MemoryStream([9, 9]), GueltigesErgebnis());

        Assert.True(id > 0);
        Assert.Contains("test.pdf", blob.HochgeladeneDateinamen);
        Assert.Equal(1, await db.StuecklistenpruefungVerlaufEintraege.CountAsync());
    }

    [Fact]
    public async Task ListeAsync_LiefertNurEintraegeDesUsers()
    {
        var db = NeueDb();
        var service = new StuecklistenpruefungVerlaufService(db, new FakeBlobStorageService());
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();

        await service.SpeichernAsync(userA, "a.pdf", new MemoryStream([1]), GueltigesErgebnis());
        await service.SpeichernAsync(userB, "b.pdf", new MemoryStream([2]), GueltigesErgebnis());

        var liste = await service.ListeAsync(userA);

        var eintrag = Assert.Single(liste);
        Assert.Equal("a.pdf", eintrag.Dateiname);
    }

    [Fact]
    public async Task ListeAsync_OhneUserfilter_LiefertAlleEintraegeMitUploader()
    {
        var db = NeueDb();
        var service = new StuecklistenpruefungVerlaufService(db, new FakeBlobStorageService());
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        db.UserProfiles.AddRange(
            new UserProfile { Id = userA, ObjectId = userA.ToString(), DisplayName = "Anna Beispiel" },
            new UserProfile { Id = userB, ObjectId = userB.ToString(), DisplayName = "Bernd Beispiel" });
        await db.SaveChangesAsync();

        await service.SpeichernAsync(userA, "a.pdf", new MemoryStream([1]), GueltigesErgebnis());
        await service.SpeichernAsync(userB, "b.pdf", new MemoryStream([2]), GueltigesErgebnis());

        var liste = await service.ListeAsync();

        Assert.Equal(2, liste.Count);
        Assert.Contains(liste, e => e.Dateiname == "a.pdf" && e.HochgeladenVon == "Anna Beispiel");
        Assert.Contains(liste, e => e.Dateiname == "b.pdf" && e.HochgeladenVon == "Bernd Beispiel");
    }

    [Fact]
    public async Task DetailAsync_LiefertNull_WennFremderUser()
    {
        var db = NeueDb();
        var service = new StuecklistenpruefungVerlaufService(db, new FakeBlobStorageService());
        var eigentuemer = Guid.NewGuid();
        var fremder = Guid.NewGuid();

        var id = await service.SpeichernAsync(eigentuemer, "a.pdf", new MemoryStream([1]), GueltigesErgebnis());

        Assert.Null(await service.DetailAsync(fremder, id));
        Assert.NotNull(await service.DetailAsync(eigentuemer, id));
    }

    [Fact]
    public async Task DetailAsync_LiefertGespeicherteMerkmaleKorrekt()
    {
        var db = NeueDb();
        var service = new StuecklistenpruefungVerlaufService(db, new FakeBlobStorageService());
        var userId = Guid.NewGuid();

        var id = await service.SpeichernAsync(userId, "a.pdf", new MemoryStream([1]), GueltigesErgebnis());
        var detail = await service.DetailAsync(userId, id);

        Assert.NotNull(detail);
        var merkmal = Assert.Single(detail!.Merkmale);
        Assert.Equal("9209425", merkmal.Merkmalsnummer);
    }

    [Fact]
    public async Task DokumentAsync_LiefertNull_WennFremderUser()
    {
        var db = NeueDb();
        var service = new StuecklistenpruefungVerlaufService(db, new FakeBlobStorageService());
        var eigentuemer = Guid.NewGuid();
        var fremder = Guid.NewGuid();

        var id = await service.SpeichernAsync(eigentuemer, "a.pdf", new MemoryStream([1]), GueltigesErgebnis());

        Assert.Null(await service.DokumentAsync(fremder, id));
        Assert.NotNull(await service.DokumentAsync(eigentuemer, id));
    }

    [Fact]
    public async Task StandAktualisierenAsync_SpeichertSchrittStuecklisteUndVergleich()
    {
        var db = NeueDb();
        var service = new StuecklistenpruefungVerlaufService(db, new FakeBlobStorageService());
        var userId = Guid.NewGuid();
        var id = await service.SpeichernAsync(userId, "a.pdf", new MemoryStream([1]), GueltigesErgebnis());

        var stueckliste = new StuecklistenKnoten("100", "Wurzel", 1, "ST", []);
        var vergleich = new VergleichsErgebnis(
            new VergleichsKnoten(
                "100", "Wurzel", 1, "ST", 1, "ST",
                VergleichsStatus.Uebereinstimmung, null, []),
            []);
        var geaenderteMerkmale =
            new[] { new ErkanntesMerkmal("50", "9999999", "Manuell ergänzt") };

        var aktualisiert = await service.StandAktualisierenAsync(
            userId,
            id,
            new VerlaufStandAktualisieren(4, geaenderteMerkmale, [], stueckliste, vergleich, "sap.txt"));
        var detail = await service.DetailAsync(userId, id);

        Assert.True(aktualisiert);
        Assert.NotNull(detail);
        Assert.Equal(4, detail!.ErreichterSchritt);
        Assert.Equal("100", detail.Stueckliste!.Artikelnummer);
        Assert.Equal(VergleichsStatus.Uebereinstimmung, detail.VergleichsErgebnis!.Wurzel.Status);
        Assert.Equal("sap.txt", detail.SapDateiname);
        Assert.Equal("9999999", Assert.Single(detail.Merkmale).Merkmalsnummer);
    }

    [Fact]
    public async Task StandAktualisierenAsync_SharePointEintragOhneUser_UebernimmtBearbeiterUndSpeichert()
    {
        var db = NeueDb();
        var service = new StuecklistenpruefungVerlaufService(db, new FakeBlobStorageService());
        var user = Guid.NewGuid();

        // Unbeanspruchte SharePoint-Auftragsinformation (keine UserProfileId, kein Blob).
        var eintrag = new StuecklistenpruefungVerlaufEintrag
        {
            Quelle = AuftragsdokumentQuelle.SharePoint,
            Dateiname = "sp.pdf",
            Auftragsnummer = "11055894 / 40",
            Kundennummer = "717220",
            Maschinentyp = "RDM 75Kc",
            SharePointDriveId = "d",
            SharePointItemId = "i",
            AnalyseStatus = AuftragsdokumentAnalyseStatus.Erfolgreich,
            ErstelltAm = DateTime.UtcNow,
        };
        db.StuecklistenpruefungVerlaufEintraege.Add(eintrag);
        await db.SaveChangesAsync();

        var stueckliste = new StuecklistenKnoten("100", "W", 1, "ST", []);
        var vergleich = new VergleichsErgebnis(
            new VergleichsKnoten("100", "W", 1, "ST", 1, "ST", VergleichsStatus.Uebereinstimmung, null, []), []);

        var ok = await service.StandAktualisierenAsync(
            user, eintrag.Id,
            new VerlaufStandAktualisieren(4, GueltigesErgebnis().Merkmale, [], stueckliste, vergleich, "sap.txt"));

        Assert.True(ok);
        var aktualisiert = await db.StuecklistenpruefungVerlaufEintraege.SingleAsync(e => e.Id == eintrag.Id);
        Assert.Equal(user, aktualisiert.UserProfileId);                        // Bearbeiter übernommen
        Assert.Equal(AuftragsdokumentQuelle.SharePoint, aktualisiert.Quelle);   // bleibt SharePoint-Quelle
        Assert.Equal(4, aktualisiert.ErreichterSchritt);

        var detail = await service.DetailAsync(user, eintrag.Id);
        Assert.NotNull(detail);
        Assert.Equal("100", detail!.Stueckliste!.Artikelnummer);
    }

    [Fact]
    public async Task StandAktualisierenAsync_AendertKeinenFremdenEintrag()
    {
        var db = NeueDb();
        var service = new StuecklistenpruefungVerlaufService(db, new FakeBlobStorageService());
        var eigentuemer = Guid.NewGuid();
        var id = await service.SpeichernAsync(eigentuemer, "a.pdf", new MemoryStream([1]), GueltigesErgebnis());

        var aktualisiert = await service.StandAktualisierenAsync(
            Guid.NewGuid(),
            id,
            new VerlaufStandAktualisieren(4, [], [], null, null, null));

        Assert.False(aktualisiert);
        Assert.Equal(2, (await service.DetailAsync(eigentuemer, id))!.ErreichterSchritt);
    }

    [Fact]
    public async Task StandAktualisierenAsync_NeuerAufbau_EntferntAltesVergleichsergebnis()
    {
        var db = NeueDb();
        var service = new StuecklistenpruefungVerlaufService(db, new FakeBlobStorageService());
        var userId = Guid.NewGuid();
        var id = await service.SpeichernAsync(userId, "a.pdf", new MemoryStream([1]), GueltigesErgebnis());
        var stueckliste = new StuecklistenKnoten("100", "Wurzel", 1, "ST", []);
        var vergleich = new VergleichsErgebnis(
            new VergleichsKnoten(
                "100", "Wurzel", 1, "ST", 1, "ST",
                VergleichsStatus.Uebereinstimmung, null, []),
            []);

        await service.StandAktualisierenAsync(
            userId,
            id,
            new VerlaufStandAktualisieren(4, GueltigesErgebnis().Merkmale, [], stueckliste, vergleich, "sap.txt"));
        await service.StandAktualisierenAsync(
            userId,
            id,
            new VerlaufStandAktualisieren(3, GueltigesErgebnis().Merkmale, [], stueckliste, null, null));

        var detail = await service.DetailAsync(userId, id);

        Assert.NotNull(detail);
        Assert.Equal(3, detail!.ErreichterSchritt);
        Assert.Null(detail.VergleichsErgebnis);
        Assert.Null(detail.SapDateiname);
    }

    [Fact]
    public async Task NeuEinlesenAsync_ErsetztDokumentUndMerkmale_UndSetztFolgeschritteZurueck()
    {
        var db = NeueDb();
        var blob = new FakeBlobStorageService();
        var service = new StuecklistenpruefungVerlaufService(db, blob);
        var userId = Guid.NewGuid();
        var id = await service.SpeichernAsync(userId, "alt.pdf", new MemoryStream([1]), GueltigesErgebnis());
        var stueckliste = new StuecklistenKnoten("100", "Wurzel", 1, "ST", []);
        var vergleich = new VergleichsErgebnis(
            new VergleichsKnoten(
                "100", "Wurzel", 1, "ST", 1, "ST",
                VergleichsStatus.Uebereinstimmung, null, []),
            []);
        await service.StandAktualisierenAsync(
            userId,
            id,
            new VerlaufStandAktualisieren(4, GueltigesErgebnis().Merkmale, [], stueckliste, vergleich, "sap.txt"));
        var frisch = GueltigesErgebnis() with
        {
            Merkmale = [new ErkanntesMerkmal("40/80", "017364", "Unterheizung")]
        };

        var ersetzt = await service.NeuEinlesenAsync(
            userId, id, "neu.pdf", new MemoryStream([2]), frisch);

        var detail = await service.DetailAsync(userId, id);
        var eintrag = await db.StuecklistenpruefungVerlaufEintraege.SingleAsync(e => e.Id == id);
        Assert.True(ersetzt);
        Assert.Equal("neu.pdf", eintrag.Dateiname);
        Assert.Equal("blob/neu.pdf", eintrag.BlobPfad);
        Assert.Equal(2, detail!.ErreichterSchritt);
        Assert.Equal("017364", Assert.Single(detail.Merkmale).Merkmalsnummer);
        Assert.Null(detail.Stueckliste);
        Assert.Null(detail.VergleichsErgebnis);
        Assert.Null(detail.SapDateiname);
        Assert.Contains("blob/alt.pdf", blob.GeloeschteBlobPfade);
    }

    [Fact]
    public async Task NeuEinlesenAsync_LoeschtGeteiltenAltenBlobNicht()
    {
        var db = NeueDb();
        var blob = new FakeBlobStorageService();
        var service = new StuecklistenpruefungVerlaufService(db, blob);
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();

        var (idA, _) = await service.SpeichernOderOeffnenAsync(
            userA, "quelle.pdf", new MemoryStream([1]), GueltigesErgebnis());
        var (idB, _) = await service.SpeichernOderOeffnenAsync(
            userB, "kopie.pdf", new MemoryStream([2]), GueltigesErgebnis());

        var ersetzt = await service.NeuEinlesenAsync(
            userB, idB, "neu.pdf", new MemoryStream([3]), GueltigesErgebnis());

        Assert.True(ersetzt);
        Assert.NotEqual(idA, idB);
        Assert.DoesNotContain("blob/quelle.pdf", blob.GeloeschteBlobPfade);
        Assert.Equal("blob/quelle.pdf", (await db.StuecklistenpruefungVerlaufEintraege.FindAsync(idA))!.BlobPfad);
        Assert.Equal("blob/neu.pdf", (await db.StuecklistenpruefungVerlaufEintraege.FindAsync(idB))!.BlobPfad);
    }

    [Fact]
    public async Task SpeichernOderOeffnenAsync_GanzNeuerAuftrag_SpeichertMitBlobUndKunde()
    {
        var db = NeueDb();
        var blob = new FakeBlobStorageService();
        var service = new StuecklistenpruefungVerlaufService(db, blob, new KundenstammService(db));

        var (id, bestehend) = await service.SpeichernOderOeffnenAsync(
            Guid.NewGuid(), "a.pdf", new MemoryStream([1]), PlaszomErgebnis());

        Assert.Null(bestehend);
        Assert.True(id > 0);
        Assert.Single(blob.HochgeladeneDateinamen);
        Assert.Equal(1, await db.StuecklistenpruefungVerlaufEintraege.CountAsync());
        Assert.Equal(1, await db.Kunden.CountAsync());
        Assert.Equal(1, await db.KundenQuellen.CountAsync());
    }

    [Fact]
    public async Task SpeichernOderOeffnenAsync_EigenerReUpload_OeffnetBestehenden_LegtNichtsNeuesAn()
    {
        var db = NeueDb();
        var blob = new FakeBlobStorageService();
        var service = new StuecklistenpruefungVerlaufService(db, blob, new KundenstammService(db));
        var userId = Guid.NewGuid();

        var (ersteId, _) = await service.SpeichernOderOeffnenAsync(userId, "a.pdf", new MemoryStream([1]), PlaszomErgebnis());
        var (zweiteId, bestehend) = await service.SpeichernOderOeffnenAsync(userId, "a-nochmal.pdf", new MemoryStream([2]), PlaszomErgebnis());

        Assert.Equal(ersteId, zweiteId);
        Assert.NotNull(bestehend);
        Assert.Equal(ersteId, bestehend!.Id);
        Assert.Single(blob.HochgeladeneDateinamen);
        Assert.Equal(1, await db.StuecklistenpruefungVerlaufEintraege.CountAsync());
        Assert.Equal(1, await db.KundenQuellen.CountAsync());
    }

    [Fact]
    public async Task SpeichernOderOeffnenAsync_EigenerReUpload_LiefertGespeichertenFortschritt()
    {
        var db = NeueDb();
        var service = new StuecklistenpruefungVerlaufService(db, new FakeBlobStorageService(), new KundenstammService(db));
        var userId = Guid.NewGuid();
        var (id, _) = await service.SpeichernOderOeffnenAsync(userId, "a.pdf", new MemoryStream([1]), GueltigesErgebnis());

        var stueckliste = new StuecklistenKnoten("100", "Wurzel", 1, "ST", []);
        await service.StandAktualisierenAsync(userId, id,
            new VerlaufStandAktualisieren(3, GueltigesErgebnis().Merkmale, [], stueckliste, null, null));

        var (_, bestehend) = await service.SpeichernOderOeffnenAsync(userId, "a.pdf", new MemoryStream([1]), GueltigesErgebnis());

        Assert.NotNull(bestehend);
        Assert.Equal(3, bestehend!.ErreichterSchritt);
        Assert.Equal("100", bestehend.Stueckliste!.Artikelnummer);
    }

    [Fact]
    public async Task SpeichernOderOeffnenAsync_FremderAuftrag_LegtEigenenEintragAn_OhneBlobUndOhneNeueQuelle()
    {
        var db = NeueDb();
        var blob = new FakeBlobStorageService();
        var service = new StuecklistenpruefungVerlaufService(db, blob, new KundenstammService(db));
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();

        var (idA, _) = await service.SpeichernOderOeffnenAsync(userA, "a.pdf", new MemoryStream([1]), PlaszomErgebnis());
        var (idB, bestehendB) = await service.SpeichernOderOeffnenAsync(userB, "b.pdf", new MemoryStream([2]), PlaszomErgebnis());

        Assert.Null(bestehendB);
        Assert.NotEqual(idA, idB);
        Assert.Single(blob.HochgeladeneDateinamen);                 // nur A's Upload
        Assert.Equal(2, await db.StuecklistenpruefungVerlaufEintraege.CountAsync());
        Assert.Equal(1, await db.Kunden.CountAsync());
        Assert.Equal(1, await db.KundenQuellen.CountAsync());        // keine zweite Quelle

        var eintragA = await db.StuecklistenpruefungVerlaufEintraege.SingleAsync(e => e.Id == idA);
        var eintragB = await db.StuecklistenpruefungVerlaufEintraege.SingleAsync(e => e.Id == idB);
        Assert.Equal(eintragA.BlobPfad, eintragB.BlobPfad);          // Blob wiederverwendet
        Assert.Equal(eintragA.KundeId, eintragB.KundeId);           // Kunde übernommen
        Assert.Equal(userB, eintragB.UserProfileId);
        Assert.Equal(2, eintragB.ErreichterSchritt);
        Assert.Equal("b.pdf", eintragB.Dateiname);

        var merkmaleB = await db.VerlaufMerkmale.Where(m => m.VerlaufEintragId == idB).ToListAsync();
        Assert.Single(merkmaleB);                                    // eigene, frisch geparste Merkmale
    }

    [Fact]
    public async Task SpeichernOderOeffnenAsync_SharePointEintragVorhanden_LegtEigenenDragAndDropEintragMitBlobAn()
    {
        var db = NeueDb();
        var blob = new FakeBlobStorageService();
        var service = new StuecklistenpruefungVerlaufService(db, blob, new KundenstammService(db));
        var ergebnis = GueltigesErgebnis();
        var userId = Guid.NewGuid();

        // SharePoint hat denselben Auftrag bereits (täglich) synchronisiert — KEIN Blob, älter als
        // der Drag&Drop. Der Drag&Drop-Pfad darf diesen Eintrag NICHT als Blob-Quelle wiederverwenden.
        db.StuecklistenpruefungVerlaufEintraege.Add(new StuecklistenpruefungVerlaufEintrag
        {
            Quelle = AuftragsdokumentQuelle.SharePoint,
            Dateiname = "sharepoint.pdf",
            Auftragsnummer = ergebnis.Auftragsnummer,
            Kundennummer = ergebnis.Kundennummer,
            Maschinentyp = ergebnis.Maschinentyp,
            SharePointDriveId = "drive-1",
            SharePointItemId = "item-1",
            AnalyseStatus = AuftragsdokumentAnalyseStatus.Erfolgreich,
            SharePointGeaendertAm = new DateTime(2026, 1, 1),
            ErstelltAm = new DateTime(2026, 1, 1),
        });
        await db.SaveChangesAsync();

        var (id, bestehend) = await service.SpeichernOderOeffnenAsync(
            userId, "dragdrop.pdf", new MemoryStream([1]), ergebnis);

        Assert.Null(bestehend);
        var dragDrop = await db.StuecklistenpruefungVerlaufEintraege.SingleAsync(e => e.Id == id);
        Assert.Equal(AuftragsdokumentQuelle.DragAndDrop, dragDrop.Quelle);
        // Eigenes Dokument muss gespeichert und abrufbar sein (nicht der leere SharePoint-Blob).
        Assert.False(string.IsNullOrEmpty(dragDrop.BlobPfad));
        Assert.Contains("dragdrop.pdf", blob.HochgeladeneDateinamen);
        Assert.NotNull(await service.DokumentAsync(userId, id));
    }

    [Fact]
    public async Task SpeichernOderOeffnenAsync_MehrereEigeneTreffer_OeffnetWeitestFortgeschrittenen()
    {
        var db = NeueDb();
        var service = new StuecklistenpruefungVerlaufService(db, new FakeBlobStorageService(), new KundenstammService(db));
        var userId = Guid.NewGuid();

        // Zwei Alt-Duplikate desselben Users (direkt über SpeichernAsync — so wie sie vor dieser Änderung entstanden).
        await service.SpeichernAsync(userId, "a1.pdf", new MemoryStream([1]), GueltigesErgebnis());
        var idHoch = await service.SpeichernAsync(userId, "a2.pdf", new MemoryStream([2]), GueltigesErgebnis());
        await service.StandAktualisierenAsync(userId, idHoch,
            new VerlaufStandAktualisieren(
                4, GueltigesErgebnis().Merkmale, [],
                new StuecklistenKnoten("100", "W", 1, "ST", []),
                new VergleichsErgebnis(
                    new VergleichsKnoten("100", "W", 1, "ST", 1, "ST", VergleichsStatus.Uebereinstimmung, null, []), []),
                "sap.txt"));

        var (id, bestehend) = await service.SpeichernOderOeffnenAsync(userId, "a3.pdf", new MemoryStream([3]), GueltigesErgebnis());

        Assert.Equal(idHoch, id);
        Assert.NotNull(bestehend);
        Assert.Equal(4, bestehend!.ErreichterSchritt);
    }

    [Fact]
    public async Task SpeichernOderOeffnenAsync_GleicheAuftragsnummerAndereKundennummer_GiltAlsNeu()
    {
        var db = NeueDb();
        var blob = new FakeBlobStorageService();
        var service = new StuecklistenpruefungVerlaufService(db, blob, new KundenstammService(db));
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();

        var ergebnisKundeX = GueltigesErgebnis();                       // Kundennummer 708555
        var ergebnisKundeY = ergebnisKundeX with { Kundennummer = "999999" };

        await service.SpeichernOderOeffnenAsync(userA, "a.pdf", new MemoryStream([1]), ergebnisKundeX);
        var (_, bestehend) = await service.SpeichernOderOeffnenAsync(userB, "b.pdf", new MemoryStream([2]), ergebnisKundeY);

        Assert.Null(bestehend);                                          // kein Treffer -> neu
        Assert.Equal(2, blob.HochgeladeneDateinamen.Count);
        Assert.Equal(2, await db.StuecklistenpruefungVerlaufEintraege.CountAsync());
    }

    [Fact]
    public async Task SucheNachAuftragsnummerAsync_DragAndDropTreffer_LiefertDetailMitQuelle()
    {
        var db = NeueDb();
        var service = new StuecklistenpruefungVerlaufService(db, new FakeBlobStorageService(), new KundenstammService(db));
        var ergebnis = GueltigesErgebnis();

        await service.SpeichernAsync(Guid.NewGuid(), "a.pdf", new MemoryStream([1]), ergebnis);

        var detail = await service.SucheNachAuftragsnummerAsync(ergebnis.Auftragsnummer);

        Assert.NotNull(detail);
        Assert.Equal(AuftragsdokumentQuelle.DragAndDrop, detail!.Quelle);
        Assert.Equal("a.pdf", detail.Dateiname);
    }

    [Fact]
    public async Task SucheNachAuftragsnummerAsync_KeinTreffer_LiefertNull()
    {
        var db = NeueDb();
        var service = new StuecklistenpruefungVerlaufService(db, new FakeBlobStorageService(), new KundenstammService(db));

        var detail = await service.SucheNachAuftragsnummerAsync("nicht-vorhanden");

        Assert.Null(detail);
    }

    [Fact]
    public async Task SucheNachAuftragsnummerAsync_IgnoriertGrossKleinschreibungUndWhitespace()
    {
        var db = NeueDb();
        var service = new StuecklistenpruefungVerlaufService(db, new FakeBlobStorageService(), new KundenstammService(db));
        var ergebnis = GueltigesErgebnis();

        await service.SpeichernAsync(Guid.NewGuid(), "a.pdf", new MemoryStream([1]), ergebnis);

        var detail = await service.SucheNachAuftragsnummerAsync($"  {ergebnis.Auftragsnummer.ToLowerInvariant()}  ");

        Assert.NotNull(detail);
    }

    [Fact]
    public async Task SucheNachAuftragsnummerAsync_SharePointUndDragAndDropTreffer_BevorzugtSharePoint()
    {
        var db = NeueDb();
        var service = new StuecklistenpruefungVerlaufService(db, new FakeBlobStorageService(), new KundenstammService(db));
        var ergebnis = GueltigesErgebnis();

        await service.SpeichernAsync(Guid.NewGuid(), "dragdrop.pdf", new MemoryStream([1]), ergebnis);

        db.StuecklistenpruefungVerlaufEintraege.Add(new StuecklistenpruefungVerlaufEintrag
        {
            Quelle = AuftragsdokumentQuelle.SharePoint,
            Dateiname = "sharepoint.pdf",
            Auftragsnummer = ergebnis.Auftragsnummer,
            Kundennummer = ergebnis.Kundennummer,
            Maschinentyp = ergebnis.Maschinentyp,
            WebUrl = "https://sharepoint.example/sharepoint.pdf",
            SharePointDriveId = "drive-1",
            SharePointItemId = "item-1",
            AnalyseStatus = AuftragsdokumentAnalyseStatus.Erfolgreich,
            SharePointGeaendertAm = DateTime.UtcNow,
            ErstelltAm = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var detail = await service.SucheNachAuftragsnummerAsync(ergebnis.Auftragsnummer);

        Assert.NotNull(detail);
        Assert.Equal(AuftragsdokumentQuelle.SharePoint, detail!.Quelle);
        Assert.Equal("sharepoint.pdf", detail.Dateiname);
    }

    [Fact]
    public async Task DokumentAsync_LiefertSharePointDokument_WennQuelleSharePointIst()
    {
        var db = NeueDb();
        var eintrag = new StuecklistenpruefungVerlaufEintrag
        {
            Quelle = AuftragsdokumentQuelle.SharePoint,
            Dateiname = "sp-test.pdf",
            WebUrl = "https://example.test/sp-test.pdf",
            Auftragsnummer = "11055894 / 40",
            Kundennummer = "717220",
            Maschinentyp = "RDM 75Kc",
            SharePointDriveId = "drive-1",
            SharePointItemId = "item-1",
            AnalyseStatus = AuftragsdokumentAnalyseStatus.Erfolgreich,
            ErstelltAm = DateTime.UtcNow,
        };
        db.StuecklistenpruefungVerlaufEintraege.Add(eintrag);
        await db.SaveChangesAsync();

        var sharePointClient = new FakeSharePointDokumentClient();
        var service = new StuecklistenpruefungVerlaufService(db, new FakeBlobStorageService(), sharePointClient: sharePointClient);

        var doc = await service.DokumentAsync(eintrag.Id);

        Assert.NotNull(doc);
        Assert.Equal("sp-test.pdf", doc!.Value.Dateiname);
        Assert.Equal("https://example.test/sp-test.pdf", sharePointClient.GeoeffneteWebUrl);
        using var ms = new MemoryStream();
        await doc.Value.Inhalt.CopyToAsync(ms);
        Assert.Equal([7, 8, 9], ms.ToArray());
    }
}
