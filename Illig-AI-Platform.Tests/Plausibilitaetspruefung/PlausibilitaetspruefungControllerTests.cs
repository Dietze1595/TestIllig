using Illig_AI_Platform.Controllers.Plausibilitaetspruefung;
using Illig_AI_Platform.Shared.Data;
using Illig_AI_Platform.Shared.SapImport;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Illig_AI_Platform.Tests.Plausibilitaetspruefung;

public class PlausibilitaetspruefungControllerTests
{
    private static (PlausibilitaetspruefungController Controller, AppDbContext Db) CreateController()
    {
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        var service = new SapDirektImportService(db, NullLogger<SapDirektImportService>.Instance);
        return (new PlausibilitaetspruefungController(service), db);
    }

    [Fact]
    public async Task Stuecklisten_ImportiertHierarchieDirekt()
    {
        var (controller, db) = CreateController();
        var push = new StuecklistenPush
        {
            BomType = "ORDER_BOM",
            OrderNumber = "11055894",
            OrderItem = "40",
            ValidAt = new DateOnly(2026, 7, 8),
            RootNodeId = "root",
            Nodes =
            [
                new() { NodeId = "root", Type = "ASSEMBLY", MaterialNumber = "9209307", Quantity = 1, Unit = "ST" },
                new() { NodeId = "child", ParentNodeId = "root", Position = "0100", Type = "MATERIAL", MaterialNumber = "8104330", Quantity = 1.91m, Unit = "M2" }
            ]
        };

        var result = await controller.Stuecklisten(push, default);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(2, Assert.IsType<SapDirektImportErgebnis>(ok.Value).ZeilenAnzahl);
        var child = Assert.Single(db.StuecklistenPositionen, position => position.NodeId == "child");
        Assert.Equal("root", child.ParentNodeId);
        Assert.Equal("40", child.Auftragsposition);
    }

    [Fact]
    public async Task Maximalstuecklisten_ImportiertDokumentknotenUndElternbeziehung()
    {
        var (controller, db) = CreateController();
        var push = new MaximalstuecklistenPush
        {
            BomType = "MAXIMUM_MATERIAL_BOM",
            MaterialNumber = "9209307",
            Description = "RDM 75Kc",
            Plant = "0001",
            BomUsage = "1",
            BomAlternative = "01",
            ValidAt = new DateOnly(2026, 7, 8),
            Nodes =
            [
                new() { NodeId = "100000", Type = "ASSEMBLY", MaterialNumber = "9209307", Quantity = 1, Unit = "ST" },
                new()
                {
                    NodeId = "100030", ParentNodeId = "100000", Position = "0020", Type = "DOCUMENT",
                    Description = "Strahlerplan", Quantity = 1, Unit = "ST",
                    Document = new() { DocumentType = "ZTE", DocumentNumber = "P 877", Version = "A" }
                }
            ]
        };

        var result = await controller.Maximalstuecklisten(push, default);

        Assert.IsType<OkObjectResult>(result.Result);
        var root = Assert.Single(db.MaximalstuecklistenPositionen, position => position.ParentId is null);
        var document = Assert.Single(db.MaximalstuecklistenPositionen, position => position.Typ == "DOCUMENT");
        Assert.Equal(root.Id, document.ParentId);
        Assert.Equal("P 877", document.Dokumentnummer);
        Assert.Equal("0020", document.Artikelnummer);
    }

    [Fact]
    public async Task Stuecklisten_LeereNodesWerdenAbgelehnt()
    {
        var (controller, _) = CreateController();
        var push = new StuecklistenPush
        {
            BomType = "ORDER_BOM", OrderNumber = "1", OrderItem = "10", RootNodeId = "root"
        };

        var result = await controller.Stuecklisten(push, default);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }
}
