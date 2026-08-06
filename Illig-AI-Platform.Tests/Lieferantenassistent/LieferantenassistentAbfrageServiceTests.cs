using Illig_AI_Platform.Shared.Data;
using Illig_AI_Platform.Shared.Lieferantenassistent;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Illig_AI_Platform.Tests.Lieferantenassistent;

public class LieferantenassistentAbfrageServiceTests
{
    private static readonly DateOnly Heute = new(2026, 7, 17);

    private static AppDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static async Task SeedeAsync(AppDbContext db)
    {
        db.Lieferanten.Add(new Lieferant { Kreditor = 1051, Name = "R+W Antriebselemente GmbH", AdressNummer = 12345 });
        db.Lieferanten.Add(new Lieferant { Kreditor = 1088, Name = "Harmonic Drive AG", AdressNummer = 67890 });
        db.LieferantEmailAdressen.Add(new LieferantEmailAdresse { AdressNummer = 12345, EmailAdresse = "standard@rw.de", IstStandard = true });
        db.LieferantEmailAdressen.Add(new LieferantEmailAdresse { AdressNummer = 12345, EmailAdresse = "andere@rw.de", IstStandard = false });
        db.Dispositionspositionen.Add(new Dispositionsposition
        {
            Einkaufsbeleg = "45647126", Position = "10", LieferantKreditor = 1051,
            Material = "9152207", Kurztext = "Kupplung", Bestellmenge = 2, NochZuLiefernMenge = 2,
            Lieferdatum = Heute.AddDays(-1), ImportiertAm = DateTime.UtcNow
        });
        db.Dispositionspositionen.Add(new Dispositionsposition
        {
            Einkaufsbeleg = "45644415", Position = "10", LieferantKreditor = 1088,
            Material = "9238159", Kurztext = "Servomotor", Bestellmenge = 2, NochZuLiefernMenge = 0,
            Lieferdatum = Heute.AddDays(30), ImportiertAm = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task OffenePositionenAsync_FiltertVollstaendigGelieferteAus()
    {
        await using var db = CreateDb();
        await SeedeAsync(db);
        var service = new LieferantenassistentAbfrageService(db);

        var ergebnis = await service.OffenePositionenAsync(null, null, Heute);

        var position = Assert.Single(ergebnis);
        Assert.Equal("45647126", position.Einkaufsbeleg);
    }

    [Fact]
    public async Task OffenePositionenAsync_BerechnetAmpelStatusUndMapptEmailAdressen()
    {
        await using var db = CreateDb();
        await SeedeAsync(db);
        var service = new LieferantenassistentAbfrageService(db);

        var position = Assert.Single(await service.OffenePositionenAsync(null, null, Heute));

        Assert.Equal(LieferterminStatus.Ueberfaellig, position.Status);
        Assert.Equal("R+W Antriebselemente GmbH", position.LieferantName);
        Assert.Equal(2, position.EmailAdressen.Count);
        Assert.Contains(position.EmailAdressen, e => e.EmailAdresse == "standard@rw.de" && e.IstStandard);
    }

    [Fact]
    public async Task OffenePositionenAsync_SucheFiltertNachLieferantMaterialKurztextUndEinkaufsbeleg()
    {
        await using var db = CreateDb();
        await SeedeAsync(db);
        var service = new LieferantenassistentAbfrageService(db);

        Assert.Single(await service.OffenePositionenAsync("R+W", null, Heute));
        Assert.Single(await service.OffenePositionenAsync("9152207", null, Heute));
        Assert.Single(await service.OffenePositionenAsync("Kupplung", null, Heute));
        Assert.Single(await service.OffenePositionenAsync("  Kupplung  ", null, Heute));
        Assert.Single(await service.OffenePositionenAsync("45647126", null, Heute));
        Assert.Empty(await service.OffenePositionenAsync("Unbekannt", null, Heute));
    }

    [Fact]
    public async Task OffenePositionenAsync_StatusFilterGrenztEin()
    {
        await using var db = CreateDb();
        await SeedeAsync(db);
        var service = new LieferantenassistentAbfrageService(db);

        Assert.Single(await service.OffenePositionenAsync(null, [LieferterminStatus.Ueberfaellig], Heute));
        Assert.Empty(await service.OffenePositionenAsync(null, [LieferterminStatus.ImPlan], Heute));
    }

    [Fact]
    public async Task OffenePositionenAsync_LieferantOhneAdressNummerLiefertKeineEmailAdressen()
    {
        await using var db = CreateDb();
        db.Lieferanten.Add(new Lieferant { Kreditor = 2000, Name = "Ohne Adressnummer GmbH", AdressNummer = null });
        db.Dispositionspositionen.Add(new Dispositionsposition
        {
            Einkaufsbeleg = "45699999", Position = "10", LieferantKreditor = 2000,
            Material = "1234567", Kurztext = "Testteil", Bestellmenge = 1, NochZuLiefernMenge = 1,
            Lieferdatum = Heute.AddDays(-1), ImportiertAm = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        var service = new LieferantenassistentAbfrageService(db);

        var position = Assert.Single(await service.OffenePositionenAsync(null, null, Heute));

        Assert.Empty(position.EmailAdressen);
    }

    [Fact]
    public async Task OffenePositionenAsync_EinkaeufergruppeFilterGrenztEin()
    {
        await using var db = CreateDb();
        db.Lieferanten.Add(new Lieferant { Kreditor = 1051, Name = "R+W Antriebselemente GmbH" });
        db.Dispositionspositionen.Add(new Dispositionsposition
        {
            Einkaufsbeleg = "45647126", Position = "10", LieferantKreditor = 1051,
            Einkaeufergruppe = "021", Kurztext = "Kupplung", Bestellmenge = 2, NochZuLiefernMenge = 2,
            Lieferdatum = Heute, ImportiertAm = DateTime.UtcNow
        });
        db.Dispositionspositionen.Add(new Dispositionsposition
        {
            Einkaufsbeleg = "45644415", Position = "10", LieferantKreditor = 1051,
            Einkaeufergruppe = "041", Kurztext = "Servomotor", Bestellmenge = 2, NochZuLiefernMenge = 2,
            Lieferdatum = Heute, ImportiertAm = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        var service = new LieferantenassistentAbfrageService(db);

        var gefiltert = await service.OffenePositionenAsync(null, null, Heute, ["021"]);

        var position = Assert.Single(gefiltert);
        Assert.Equal("45647126", position.Einkaufsbeleg);
    }

    [Fact]
    public async Task EinkaeufergruppenAsync_LiefertDistinkteSortierteWerteNurAusOffenenPositionen()
    {
        await using var db = CreateDb();
        db.Lieferanten.Add(new Lieferant { Kreditor = 1051, Name = "R+W Antriebselemente GmbH" });
        db.Dispositionspositionen.Add(new Dispositionsposition
        {
            Einkaufsbeleg = "45647126", Position = "10", LieferantKreditor = 1051,
            Einkaeufergruppe = "041", Kurztext = "Kupplung", Bestellmenge = 2, NochZuLiefernMenge = 2,
            Lieferdatum = Heute, ImportiertAm = DateTime.UtcNow
        });
        db.Dispositionspositionen.Add(new Dispositionsposition
        {
            Einkaufsbeleg = "45644415", Position = "10", LieferantKreditor = 1051,
            Einkaeufergruppe = "021", Kurztext = "Servomotor", Bestellmenge = 2, NochZuLiefernMenge = 2,
            Lieferdatum = Heute, ImportiertAm = DateTime.UtcNow
        });
        db.Dispositionspositionen.Add(new Dispositionsposition
        {
            // Bereits vollständig geliefert — darf nicht in der Werteliste auftauchen.
            Einkaufsbeleg = "45699999", Position = "10", LieferantKreditor = 1051,
            Einkaeufergruppe = "950", Kurztext = "Erledigt", Bestellmenge = 2, NochZuLiefernMenge = 0,
            Lieferdatum = Heute, ImportiertAm = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        var service = new LieferantenassistentAbfrageService(db);

        var gruppen = await service.EinkaeufergruppenAsync();

        Assert.Equal(["021", "041"], gruppen);
    }

    [Fact]
    public async Task OffenePositionenAsync_StatusFilterMitMehrerenWertenLiefertPositionenAllerAusgewaehltenStati()
    {
        await using var db = CreateDb();
        db.Lieferanten.Add(new Lieferant { Kreditor = 1051, Name = "R+W Antriebselemente GmbH" });
        db.Dispositionspositionen.Add(new Dispositionsposition
        {
            // Überfällig (Lieferdatum in der Vergangenheit)
            Einkaufsbeleg = "45647126", Position = "10", LieferantKreditor = 1051,
            Kurztext = "Kupplung", Bestellmenge = 2, NochZuLiefernMenge = 2,
            Lieferdatum = Heute.AddDays(-1), ImportiertAm = DateTime.UtcNow
        });
        db.Dispositionspositionen.Add(new Dispositionsposition
        {
            // Bald fällig (innerhalb 7 Tage)
            Einkaufsbeleg = "45644415", Position = "10", LieferantKreditor = 1051,
            Kurztext = "Servomotor", Bestellmenge = 2, NochZuLiefernMenge = 2,
            Lieferdatum = Heute.AddDays(3), ImportiertAm = DateTime.UtcNow
        });
        db.Dispositionspositionen.Add(new Dispositionsposition
        {
            // Im Plan (mehr als 7 Tage entfernt)
            Einkaufsbeleg = "45611111", Position = "10", LieferantKreditor = 1051,
            Kurztext = "ImPlanTeil", Bestellmenge = 2, NochZuLiefernMenge = 2,
            Lieferdatum = Heute.AddDays(30), ImportiertAm = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        var service = new LieferantenassistentAbfrageService(db);

        var gefiltert = await service.OffenePositionenAsync(
            null, [LieferterminStatus.Ueberfaellig, LieferterminStatus.BaldFaellig], Heute);

        Assert.Equal(2, gefiltert.Count);
        Assert.Contains(gefiltert, p => p.Einkaufsbeleg == "45647126");
        Assert.Contains(gefiltert, p => p.Einkaufsbeleg == "45644415");
        Assert.DoesNotContain(gefiltert, p => p.Einkaufsbeleg == "45611111");
    }

    [Fact]
    public async Task OffenePositionenAsync_EinkaeufergruppeFilterMitMehrerenWertenLiefertPositionenAllerAusgewaehltenGruppen()
    {
        await using var db = CreateDb();
        db.Lieferanten.Add(new Lieferant { Kreditor = 1051, Name = "R+W Antriebselemente GmbH" });
        db.Dispositionspositionen.Add(new Dispositionsposition
        {
            Einkaufsbeleg = "45647126", Position = "10", LieferantKreditor = 1051,
            Einkaeufergruppe = "021", Kurztext = "Kupplung", Bestellmenge = 2, NochZuLiefernMenge = 2,
            Lieferdatum = Heute, ImportiertAm = DateTime.UtcNow
        });
        db.Dispositionspositionen.Add(new Dispositionsposition
        {
            Einkaufsbeleg = "45644415", Position = "10", LieferantKreditor = 1051,
            Einkaeufergruppe = "041", Kurztext = "Servomotor", Bestellmenge = 2, NochZuLiefernMenge = 2,
            Lieferdatum = Heute, ImportiertAm = DateTime.UtcNow
        });
        db.Dispositionspositionen.Add(new Dispositionsposition
        {
            Einkaufsbeleg = "45611111", Position = "10", LieferantKreditor = 1051,
            Einkaeufergruppe = "950", Kurztext = "AndereGruppe", Bestellmenge = 2, NochZuLiefernMenge = 2,
            Lieferdatum = Heute, ImportiertAm = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        var service = new LieferantenassistentAbfrageService(db);

        var gefiltert = await service.OffenePositionenAsync(null, null, Heute, ["021", "041"]);

        Assert.Equal(2, gefiltert.Count);
        Assert.Contains(gefiltert, p => p.Einkaufsbeleg == "45647126");
        Assert.Contains(gefiltert, p => p.Einkaufsbeleg == "45644415");
        Assert.DoesNotContain(gefiltert, p => p.Einkaufsbeleg == "45611111");
    }
}
