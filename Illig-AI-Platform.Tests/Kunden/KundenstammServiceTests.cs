using Illig_AI_Platform.Shared.Auftragsanlage;
using Illig_AI_Platform.Shared.Data;
using Illig_AI_Platform.Shared.Kunden;
using Illig_AI_Platform.Shared.Plausibilitaetspruefung.Stuecklistenpruefung;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Illig_AI_Platform.Tests.Kunden;

public class KundenstammServiceTests
{
    private static AppDbContext NeueDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public async Task FindeOderErstelleAsync_FuehrtGleichenNamenUndAdresseZusammen()
    {
        var db = NeueDb();
        var service = new KundenstammService(db);

        var erster = await service.FindeOderErstelleAsync(
            "Muster GmbH", "Musterstraße 1, 74074 Heilbronn", null);
        var zweiter = await service.FindeOderErstelleAsync(
            "MUSTER", "Musterstrasse 1 · 74074 Heilbronn", null);

        Assert.NotNull(erster);
        Assert.Equal(erster!.Id, zweiter!.Id);
        Assert.Single(db.Kunden);
    }

    [Fact]
    public async Task ListeAsync_ZaehltBasisauftragUndMaschinenpositionenGetrennt()
    {
        var db = NeueDb();
        db.StuecklistenpruefungVerlaufEintraege.AddRange(
            Verlauf("11055627 / 10", "RDK 80", "708555"),
            Verlauf("11055627 / 20", "RDK 90", "708555"),
            Verlauf("11055627 / 20", "RDK 90", "708555"));
        await db.SaveChangesAsync();
        var service = new KundenstammService(db);

        var result = await service.ListeAsync();

        var kunde = Assert.Single(result);
        Assert.Equal(1, kunde.Auftraege);
        Assert.Equal(2, kunde.Maschinen);
    }

    [Fact]
    public async Task SynchronisierenAsync_ZaehltAngebotsversionenNurEinmalUndVerknuepftBestellung()
    {
        var db = NeueDb();
        var angebot1 = AngebotMitVersion(1);
        var angebot2 = AngebotMitVersion(2);
        db.Angebote.AddRange(angebot1, angebot2);
        await db.SaveChangesAsync();
        db.Auftragsbestaetigungen.Add(new Auftragsbestaetigung
        {
            AngebotId = angebot2.Id,
            Nummer = "PO-4711",
            Kundenname = "Muster GmbH",
            Kundenadresse = "Musterstraße 1, 74074 Heilbronn",
            Volltext = "",
            SonstigeAbweichungen = "",
            Dateiname = "bestellung.pdf",
            BlobPfad = "blob-bestellung",
            HochgeladenAm = new DateTime(2026, 2, 1),
        });
        await db.SaveChangesAsync();
        var service = new KundenstammService(db);

        var result = await service.ListeAsync();

        var kunde = Assert.Single(result);
        Assert.Equal(1, kunde.Angebote);
        Assert.Equal(1, kunde.Kundenbestellungen);
        Assert.All(db.Angebote, a => Assert.Equal(db.Kunden.Single().Id, a.KundeId));
        Assert.Equal(db.Kunden.Single().Id, db.Auftragsbestaetigungen.Single().KundeId);

        var detail = await service.DetailAsync(kunde.Id);
        Assert.NotNull(detail);
        Assert.Contains(detail!.Dokumente, d => d.Quelltyp == KundenQuelltyp.Angebot);
        Assert.Contains(detail.Dokumente, d => d.Quelltyp == KundenQuelltyp.Kundenbestellung);
    }

    [Fact]
    public async Task ListeAsync_FreigegebenesAngebotMitBestellung_MachtVorlaeufigenKundenZumNeukunden()
    {
        var db = NeueDb();
        var angebot = AngebotMitVersion(1);
        angebot.Freigegeben = true;
        angebot.FreigegebenAm = new DateTime(2026, 1, 15);
        db.Angebote.Add(angebot);
        await db.SaveChangesAsync();
        db.Auftragsbestaetigungen.Add(new Auftragsbestaetigung
        {
            AngebotId = angebot.Id,
            Nummer = "PO-NEU-1",
            Kundenname = angebot.Kundenname,
            Kundenadresse = angebot.Kundenadresse,
            Volltext = "",
            SonstigeAbweichungen = "",
            Dateiname = "bestellung.pdf",
            BlobPfad = "blob-bestellung",
            HochgeladenAm = new DateTime(2026, 1, 20),
        });
        await db.SaveChangesAsync();
        var service = new KundenstammService(db);

        var result = await service.ListeAsync();

        var kunde = Assert.Single(result);
        Assert.Equal(KundeStatus.Neukunde, kunde.Status);
        Assert.Equal(KundeStatus.Neukunde, db.Kunden.Single().Status);
    }

    [Fact]
    public async Task ListeAsync_NichtFreigegebenesAngebotMitBestellung_BleibtVorlaeufig()
    {
        var db = NeueDb();
        var angebot = AngebotMitVersion(1);
        db.Angebote.Add(angebot);
        await db.SaveChangesAsync();
        db.Auftragsbestaetigungen.Add(new Auftragsbestaetigung
        {
            AngebotId = angebot.Id,
            Nummer = "PO-ENTWURF-1",
            Kundenname = angebot.Kundenname,
            Kundenadresse = angebot.Kundenadresse,
            Volltext = "",
            SonstigeAbweichungen = "",
            Dateiname = "bestellung.pdf",
            BlobPfad = "blob-bestellung",
            HochgeladenAm = new DateTime(2026, 1, 20),
        });
        await db.SaveChangesAsync();
        var service = new KundenstammService(db);

        var result = await service.ListeAsync();

        Assert.Equal(KundeStatus.Vorlaeufig, Assert.Single(result).Status);
    }

    [Fact]
    public async Task ListeAsync_UebernimmtKundennameUndAdresseAusAuftragsinformation()
    {
        var db = NeueDb();
        db.StuecklistenpruefungVerlaufEintraege.Add(new StuecklistenpruefungVerlaufEintrag
        {
            UserProfileId = Guid.NewGuid(),
            Dateiname = "plaszom.pdf",
            BlobPfad = "blob-plaszom",
            Auftragsnummer = "11055783 / 60",
            Kundennummer = "712082",
            Kundenname = "PLASZOM, Orleans - SC",
            Kundenadresse = "PO Box 06 ORLEANS - SC 88870-000 BRASILIEN",
            Maschinentyp = "RS 91_konf",
            ErstelltAm = new DateTime(2026, 1, 1),
        });
        await db.SaveChangesAsync();
        var service = new KundenstammService(db);

        var result = await service.ListeAsync();

        var kunde = Assert.Single(result);
        Assert.Equal("PLASZOM, Orleans - SC", kunde.Anzeigename);
        Assert.Equal("712082", kunde.Kundennummer);
        Assert.Equal("PO Box 06 ORLEANS - SC 88870-000 BRASILIEN", kunde.Adresse);
        Assert.Equal(KundeStatus.Bestaetigt, kunde.Status);
    }

    [Fact]
    public async Task FindeOderErstelleAsync_BestehendeKundennummer_WirdWiederverwendetUndNameErgaenzt()
    {
        var db = NeueDb();
        var service = new KundenstammService(db);

        var ohneName = await service.FindeOderErstelleAsync(null, null, "712082");
        var mitName = await service.FindeOderErstelleAsync(
            "PLASZOM, Orleans - SC", "Plaszom Zomer Industrial de Plásticos Ltda", "712082");

        Assert.NotNull(ohneName);
        Assert.Equal(ohneName!.Id, mitName!.Id);
        Assert.Single(db.Kunden);
        Assert.Equal("PLASZOM, Orleans - SC", mitName.Name);
    }

    [Fact]
    public async Task DetailAsync_ZeigtGleicheAuftragsinformationNurEinmal()
    {
        var db = NeueDb();
        // Zwei Prüfläufe desselben Auftrags durch verschiedene Nutzer erzeugen zwei Verlaufszeilen
        // (siehe SpeichernOderOeffnenAsync, Fall 2), meinen aber dieselbe Auftragsinformation.
        db.StuecklistenpruefungVerlaufEintraege.AddRange(
            new StuecklistenpruefungVerlaufEintrag
            {
                UserProfileId = Guid.NewGuid(),
                Dateiname = "auftrag-nutzer-a.pdf",
                BlobPfad = "blob-auftrag",
                Auftragsnummer = "11055627 / 10",
                Kundennummer = "708555",
                Kundenname = "Malico General Trading FZCO",
                Maschinentyp = "RDK 80",
                ErstelltAm = new DateTime(2026, 1, 1),
            },
            new StuecklistenpruefungVerlaufEintrag
            {
                UserProfileId = Guid.NewGuid(),
                Dateiname = "auftrag-nutzer-b.pdf",
                BlobPfad = "blob-auftrag",
                Auftragsnummer = "11055627 / 10",
                Kundennummer = "708555",
                Kundenname = "Malico General Trading FZCO",
                Maschinentyp = "RDK 80",
                ErstelltAm = new DateTime(2026, 1, 2),
            });
        await db.SaveChangesAsync();
        var service = new KundenstammService(db);

        var kunde = Assert.Single(await service.ListeAsync());
        var detail = await service.DetailAsync(kunde.Id);

        Assert.NotNull(detail);
        var auftragsinfo = Assert.Single(
            detail!.Dokumente, d => d.Quelltyp == KundenQuelltyp.Auftragsinformation);
        // Der jüngste Eintrag bleibt als Repräsentant.
        Assert.Equal("auftrag-nutzer-b.pdf", auftragsinfo.Dateiname);
    }

    [Fact]
    public void ZerlegeAuftragsnummer_ErkenntBasisUndPosition()
    {
        var (basis, position) = KundenstammService.ZerlegeAuftragsnummer("11055627 / 20");

        Assert.Equal("11055627", basis);
        Assert.Equal("20", position);
    }

    private static StuecklistenpruefungVerlaufEintrag Verlauf(
        string auftragsnummer, string maschinentyp, string kundennummer) => new()
    {
        UserProfileId = Guid.NewGuid(),
        Dateiname = $"{auftragsnummer}.pdf",
        BlobPfad = $"blob-{auftragsnummer}",
        Auftragsnummer = auftragsnummer,
        Kundennummer = kundennummer,
        Maschinentyp = maschinentyp,
        ErstelltAm = new DateTime(2026, 1, 1),
    };

    private static Angebot AngebotMitVersion(int version) => new()
    {
        Angebotsnummer = "50001234",
        Version = version,
        Kundenname = "Muster GmbH",
        Kundenadresse = "Musterstraße 1, 74074 Heilbronn",
        Volltext = "",
        Dateiname = $"angebot-v{version}.pdf",
        BlobPfad = $"blob-v{version}",
        HochgeladenAm = new DateTime(2026, 1, version),
    };
}
