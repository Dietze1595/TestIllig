using Illig_AI_Platform.Controllers.Lieferantenassistent;
using Illig_AI_Platform.Shared.Data;
using Illig_AI_Platform.Shared.SapImport;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Illig_AI_Platform.Tests.Lieferantenassistent;

public class LieferantenassistentSapImportControllerTests
{
    private static (LieferantenassistentSapImportController Controller, AppDbContext Db) CreateController()
    {
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        var service = new SapDirektImportService(db, NullLogger<SapDirektImportService>.Instance);
        return (new LieferantenassistentSapImportController(service), db);
    }

    [Fact]
    public async Task Dispositionsliste_ImportiertScheduleLinesDirekt()
    {
        var (controller, db) = CreateController();
        var push = new OffeneBestellungenPush
        {
            PurchaseOrders =
            [
                new()
                {
                    PurchaseOrderNumber = "45647126", LabNr = "LAB-1", SupplierNumber = "2707",
                    DocumentDate = new DateOnly(2026, 7, 10), PurchasingGroup = "021", Currency = "EUR",
                    Items =
                    [
                        new()
                        {
                            Position = "00010", MaterialNumber = "9152207", Description = "Kupplung", Plant = "0001",
                            OrderQuantity = 2, Unit = "ST",
                            PurchasingGroup = new() { Code = "021", Name = "Mechanischer Einkauf" },
                            MaterialGroup = new() { Code = "100100", Name = "Antriebstechnik" },
                            ScheduleLines =
                            [
                                new() { ScheduleLineNumber = "0001", DeliveryDate = new DateOnly(2026, 8, 17), ScheduledQuantity = 2, OpenQuantity = 2 }
                            ]
                        }
                    ]
                }
            ]
        };

        var result = await controller.Dispositionsliste(push, default);

        Assert.IsType<OkObjectResult>(result.Result);
        var position = Assert.Single(db.Dispositionspositionen);
        Assert.Equal(2707, position.LieferantKreditor);
        Assert.Equal("0001", position.Einteilungsnummer);
        Assert.Equal("100100", position.MaterialgruppenCode);
        Assert.Equal("LAB-1", position.Auftragsbestaetigung);
    }

    [Fact]
    public async Task Lieferantenstammdaten_ImportiertLieferantenUndKontakteDirekt()
    {
        var (controller, db) = CreateController();
        var push = new LieferantenDatenPush
        {
            Suppliers =
            [
                new()
                {
                    SupplierNumber = "2707", Name = "Bosch Rexroth", Country = "DE", PostalCode = "70736",
                    City = "Fellbach", Street = "Siemensstrasse 1", AddressNumber = "88805",
                    Contacts = [new() { Email = "bestellung@example.de", IsDefault = true, ContactType = "ORDER" }]
                }
            ]
        };

        var result = await controller.LieferantenKreditorenStammdaten(push, default);

        Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal("Bosch Rexroth", Assert.Single(db.Lieferanten).Name);
        var kontakt = Assert.Single(db.LieferantEmailAdressen);
        Assert.Equal("ORDER", kontakt.KontaktTyp);
        Assert.True(kontakt.IstStandard);
    }
}
