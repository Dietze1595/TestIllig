using Illig_AI_Platform.Shared.Data;
using Illig_AI_Platform.Shared.Plausibilitaetspruefung.Stuecklistenpruefung;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Illig_AI_Platform.Tests.Plausibilitaetspruefung.Stuecklistenpruefung;

public class StuecklistenAufbauServiceTests
{
    private static AppDbContext CreateDb() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options);

    private static async Task<AppDbContext> DbMitBeispielbaumAsync()
    {
        var db = CreateDb();
        var stueckliste = new MaschinentypStueckliste { MaschinentypSchluessel = "RDM 75Kc", Kopfmaterial = "9209307" };
        db.MaschinentypStuecklisten.Add(stueckliste);
        await db.SaveChangesAsync();

        var wurzel = new MaximalstuecklistenPosition
        {
            MaschinentypStuecklisteId = stueckliste.Id, Artikelnummer = "9209307", Bezeichnung = "RDM 75Kc", Menge = 1, Einheit = "ST"
        };
        db.MaximalstuecklistenPositionen.Add(wurzel);
        await db.SaveChangesAsync();

        var ohneBedingung = new MaximalstuecklistenPosition
        {
            MaschinentypStuecklisteId = stueckliste.Id, ParentId = wurzel.Id,
            Artikelnummer = "9000001", Bezeichnung = "Immer dabei", Menge = 1, Einheit = "ST"
        };
        var mitBedingung = new MaximalstuecklistenPosition
        {
            MaschinentypStuecklisteId = stueckliste.Id, ParentId = wurzel.Id,
            Artikelnummer = "9237787", Bezeichnung = "Folieneinlauf_FB_200-900_konf", Menge = 1, Einheit = "ST",
            Bedingung = "020620 / 020621"
        };
        db.MaximalstuecklistenPositionen.AddRange(ohneBedingung, mitBedingung);
        await db.SaveChangesAsync();

        var kindVonBedingung = new MaximalstuecklistenPosition
        {
            MaschinentypStuecklisteId = stueckliste.Id, ParentId = mitBedingung.Id,
            Artikelnummer = "9237831", Bezeichnung = "Lichtleiter_BGR", Menge = 1, Einheit = "ST",
            Bedingung = "999999" // nur relevant, wenn der Elternknoten überhaupt enthalten ist
        };
        db.MaximalstuecklistenPositionen.Add(kindVonBedingung);
        await db.SaveChangesAsync();

        return db;
    }

    [Fact]
    public async Task Aufbauen_NimmtPositionenOhneBedingungImmerAuf()
    {
        await using var db = await DbMitBeispielbaumAsync();
        var service = new StuecklistenAufbauService(db);

        var baum = await service.AufbauenAsync("RDM 75Kc", []);

        Assert.NotNull(baum);
        Assert.Contains(baum!.Kinder, k => k.Artikelnummer == "9000001");
    }

    [Fact]
    public async Task Aufbauen_NimmtPositionMitErfuellterBedingungAuf()
    {
        await using var db = await DbMitBeispielbaumAsync();
        var service = new StuecklistenAufbauService(db);

        var baum = await service.AufbauenAsync("RDM 75Kc", ["020620"]);

        Assert.Contains(baum!.Kinder, k => k.Artikelnummer == "9237787");
    }

    [Fact]
    public async Task Aufbauen_LaesstPositionMitNichtErfuellterBedingungWeg()
    {
        await using var db = await DbMitBeispielbaumAsync();
        var service = new StuecklistenAufbauService(db);

        var baum = await service.AufbauenAsync("RDM 75Kc", []);

        Assert.DoesNotContain(baum!.Kinder, k => k.Artikelnummer == "9237787");
    }

    [Fact]
    public async Task Aufbauen_WertetKinderAusgeschlossenerPositionenNichtAus()
    {
        await using var db = await DbMitBeispielbaumAsync();
        var service = new StuecklistenAufbauService(db);

        // "9237787" ist erfüllt -> enthalten. Ihr Kind "9237831" hat die (hier künstlich nie
        // erfüllte) Bedingung "999999" -> wird korrekt weggelassen, weil die Kind-Bedingung
        // selbst nicht erfüllt ist (nicht zu verwechseln mit "Elternteil ausgeschlossen").
        var baum = await service.AufbauenAsync("RDM 75Kc", ["020620"]);

        var folieneinlauf = baum!.Kinder.Single(k => k.Artikelnummer == "9237787");
        Assert.Empty(folieneinlauf.Kinder);
    }

    [Fact]
    public async Task Aufbauen_LiefertNull_WennMaschinentypUnbekannt()
    {
        await using var db = CreateDb();
        var service = new StuecklistenAufbauService(db);

        var baum = await service.AufbauenAsync("Unbekannt", []);

        Assert.Null(baum);
    }

    [Theory]
    [InlineData("RDM 75Kc", true)]
    [InlineData("RDM 75Kc_Siemens_konf_ab_01.2013", true)]
    [InlineData("RDM 75K", false)]
    [InlineData("Unbekannt", false)]
    [InlineData("", false)]
    public async Task IstUmsetzungsmatrixVorhanden_PrueftMaschinentypstuecklisten(
        string maschinentyp,
        bool erwartet)
    {
        await using var db = await DbMitBeispielbaumAsync();
        var service = new StuecklistenAufbauService(db);

        var vorhanden = await service.IstUmsetzungsmatrixVorhandenAsync(maschinentyp);

        Assert.Equal(erwartet, vorhanden);
    }

    [Fact]
    public async Task UmsetzungsmatrixVerfuegbarkeit_LiefertVorhandeneSchluessel()
    {
        await using var db = await DbMitBeispielbaumAsync();
        db.MaschinentypStuecklisten.Add(new MaschinentypStueckliste
        {
            MaschinentypSchluessel = "RDK 80k",
            Kopfmaterial = "9209308"
        });
        await db.SaveChangesAsync();
        var service = new StuecklistenAufbauService(db);

        var verfuegbarkeit =
            await service.UmsetzungsmatrixVerfuegbarkeitAsync("Unbekannt");

        Assert.False(verfuegbarkeit.FuerMaschinentypVorhanden);
        Assert.Equal(
            ["RDK 80k", "RDM 75Kc"],
            verfuegbarkeit.VorhandeneMaschinentypSchluessel);
    }
}
