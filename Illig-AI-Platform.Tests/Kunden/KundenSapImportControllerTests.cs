using System.Linq;
using Illig_AI_Platform.Controllers.Kunden;
using Illig_AI_Platform.Shared.Data;
using Illig_AI_Platform.Shared.SapImport;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Illig_AI_Platform.Tests.Kunden;

public class KundenSapImportControllerTests
{
    private static (KundenSapImportController Controller, AppDbContext Db) CreateController()
    {
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        var service = new SapDirektImportService(db, NullLogger<SapDirektImportService>.Instance);
        return (new KundenSapImportController(service), db);
    }

    private static KundenAdressenPush GueltigerPush(string hauptkundennummer = "717216") => new()
    {
        Kunden =
        [
            new()
            {
                Hauptkundennummer = hauptkundennummer,
                Adressen =
                [
                    new() { Partnerrolle = "Auftraggeber", PartnerId = "717216", Name = "Malico General Trading", Strasse = "PO Box No. 18257, Office 1660", Ort = "Dubai", Land = "AE" },
                    new() { Partnerrolle = "Rechnungsempfänger", PartnerId = "717216", Name = "Malico General Trading", Strasse = "PO Box No. 18257, Office 1660", Ort = "Dubai", Land = "AE" },
                    new() { Partnerrolle = "Regulierer", PartnerId = "717216", Name = "Malico General Trading", Strasse = "PO Box No. 18257, Office 1660", Ort = "Dubai", Land = "AE" },
                    new() { Partnerrolle = "Vertretung", PartnerId = "2222", Name = "ILLIG Packaging solution", Strasse = "Robert Bosch Straße 10", Plz = "74081", Ort = "Heilbronn", Land = "DE" },
                    new() { Partnerrolle = "Warenempfänger", PartnerId = "717220", Name = "Al Sulaymania for the Pro", Strasse = "Hurr region, Lamalliye Industrial", Ort = "Karbala Governorate", Land = "IQ" },
                    new() { Partnerrolle = "Endkunde", PartnerId = "717220", Name = "Al Sulaymania for the Pro", Strasse = "Hurr region, Lamalliye Industrial", Ort = "Karbala Governorate", Land = "IQ" }
                ]
            }
        ]
    };

    [Fact]
    public async Task Partneradressen_ImportiertSechsRollenDirekt()
    {
        var (controller, db) = CreateController();

        var result = await controller.Partneradressen(GueltigerPush(), default);

        Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(6, db.KundenPartneradressen.Count());
        Assert.Contains(db.KundenPartneradressen, adresse =>
            adresse.Partnerrolle == "Vertretung" && adresse.PartnerId == "2222" && adresse.Ort == "Heilbronn");
    }

    [Fact]
    public async Task Partneradressen_LehntLeerenPayloadAb()
    {
        var (controller, db) = CreateController();

        var result = await controller.Partneradressen(new KundenAdressenPush(), default);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Empty(db.KundenPartneradressen);
    }

    [Fact]
    public async Task Partneradressen_LehntUnbekanntePartnerrolleAb()
    {
        var (controller, db) = CreateController();
        var push = new KundenAdressenPush
        {
            Kunden =
            [
                new()
                {
                    Hauptkundennummer = "717216",
                    Adressen = [new() { Partnerrolle = "Spediteur", PartnerId = "1", Name = "Test" }]
                }
            ]
        };

        var result = await controller.Partneradressen(push, default);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Empty(db.KundenPartneradressen);
    }

    [Fact]
    public async Task Partneradressen_LehntDoppelteRolleInnerhalbEinerGruppeAb()
    {
        var (controller, db) = CreateController();
        var push = new KundenAdressenPush
        {
            Kunden =
            [
                new()
                {
                    Hauptkundennummer = "717216",
                    Adressen =
                    [
                        new() { Partnerrolle = "Auftraggeber", PartnerId = "717216", Name = "Malico" },
                        new() { Partnerrolle = "Auftraggeber", PartnerId = "717216", Name = "Malico" }
                    ]
                }
            ]
        };

        var result = await controller.Partneradressen(push, default);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Empty(db.KundenPartneradressen);
    }

    [Fact]
    public async Task Partneradressen_LehntFehlendePflichtfelderAb()
    {
        var (controller, db) = CreateController();
        var push = new KundenAdressenPush
        {
            Kunden =
            [
                new()
                {
                    Hauptkundennummer = "717216",
                    Adressen = [new() { Partnerrolle = "Auftraggeber", PartnerId = "", Name = "" }]
                }
            ]
        };

        var result = await controller.Partneradressen(push, default);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Empty(db.KundenPartneradressen);
    }

    [Fact]
    public async Task Partneradressen_LehntDoppelteHauptkundennummerImPayloadAb()
    {
        var (controller, db) = CreateController();
        var push = new KundenAdressenPush
        {
            Kunden =
            [
                new() { Hauptkundennummer = "717216", Adressen = [new() { Partnerrolle = "Auftraggeber", PartnerId = "717216", Name = "Malico" }] },
                new() { Hauptkundennummer = "717216", Adressen = [new() { Partnerrolle = "Endkunde", PartnerId = "717220", Name = "Al Sulaymania" }] }
            ]
        };

        var result = await controller.Partneradressen(push, default);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Empty(db.KundenPartneradressen);
    }

    [Fact]
    public async Task Partneradressen_ErsetztVorhandeneAdressenDesselbenKundenBeiErneutemImport()
    {
        var (controller, db) = CreateController();
        await controller.Partneradressen(GueltigerPush(), default);

        var geaenderterPush = new KundenAdressenPush
        {
            Kunden =
            [
                new()
                {
                    Hauptkundennummer = "717216",
                    Adressen = [new() { Partnerrolle = "Auftraggeber", PartnerId = "717216", Name = "Malico General Trading", Ort = "Abu Dhabi" }]
                }
            ]
        };

        var result = await controller.Partneradressen(geaenderterPush, default);

        Assert.IsType<OkObjectResult>(result.Result);
        var adressen = db.KundenPartneradressen.Where(a => a.Hauptkundennummer == "717216").ToList();
        Assert.Single(adressen);
        Assert.Equal("Abu Dhabi", adressen[0].Ort);
    }

    [Fact]
    public async Task Partneradressen_LaesstAndereKundenBeimErneutenImportUnberuehrt()
    {
        var (controller, db) = CreateController();
        await controller.Partneradressen(GueltigerPush("717216"), default);
        await controller.Partneradressen(GueltigerPush("999999"), default);

        await controller.Partneradressen(new KundenAdressenPush
        {
            Kunden = [new() { Hauptkundennummer = "717216", Adressen = [new() { Partnerrolle = "Auftraggeber", PartnerId = "717216", Name = "Malico" }] }]
        }, default);

        Assert.Equal(6, db.KundenPartneradressen.Count(a => a.Hauptkundennummer == "999999"));
        Assert.Equal(1, db.KundenPartneradressen.Count(a => a.Hauptkundennummer == "717216"));
    }
}
