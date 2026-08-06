using System.Security.Claims;
using Illig_AI_Platform.Mcp;
using Illig_AI_Platform.Shared.Auftragsanlage;
using Illig_AI_Platform.Shared.Data;
using Illig_AI_Platform.Shared.Kunden;
using Illig_AI_Platform.Shared.Lieferantenassistent;
using Illig_AI_Platform.Shared.Plausibilitaetspruefung.Stuecklistenpruefung;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol;
using Xunit;

namespace Illig_AI_Platform.Tests.Mcp;

public class IlligKnowledgeToolsTests
{
    private static AppDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static IlligKnowledgeTools CreateTools(AppDbContext db)
    {
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("oid", "test-user")], "test")),
        };
        return new IlligKnowledgeTools(
            db,
            new HttpContextAccessor { HttpContext = context },
            NullLogger<IlligKnowledgeTools>.Instance);
    }

    [Fact]
    public async Task SearchCustomers_ReturnsOnlyMatchingCustomerAndActivityCounts()
    {
        await using var db = CreateDb();
        var customer = new Kunde
        {
            Name = "Muster Maschinenbau GmbH",
            Kundennummer = "K-100",
            Adresse = "Heilbronn",
            NormalisierterName = "MUSTER MASCHINENBAU",
            NormalisierteAdresse = "HEILBRONN",
            Status = KundeStatus.Bestaetigt,
            ErstelltAm = DateTime.UtcNow.AddDays(-3),
            AktualisiertAm = DateTime.UtcNow,
        };
        db.Kunden.AddRange(customer, new Kunde
        {
            Name = "Andere AG",
            NormalisierterName = "ANDERE",
            NormalisierteAdresse = "BERLIN",
            Status = KundeStatus.Vorlaeufig,
            ErstelltAm = DateTime.UtcNow,
            AktualisiertAm = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        db.Angebote.Add(new Angebot
        {
            KundeId = customer.Id,
            Angebotsnummer = "AN-1",
            Version = 1,
            Volltext = "Nicht ausgeben",
            Dateiname = "angebot.pdf",
            BlobPfad = "secret/angebot.pdf",
            HochgeladenAm = DateTime.UtcNow,
        });
        db.Auftragsbestaetigungen.Add(new Auftragsbestaetigung
        {
            KundeId = customer.Id,
            AngebotId = 1,
            Nummer = "KB-1",
            Volltext = "Nicht ausgeben",
            Dateiname = "bestellung.pdf",
            BlobPfad = "secret/bestellung.pdf",
            HochgeladenAm = DateTime.UtcNow,
        });
        db.StuecklistenpruefungVerlaufEintraege.Add(new StuecklistenpruefungVerlaufEintrag
        {
            KundeId = customer.Id,
            UserProfileId = Guid.NewGuid(),
            Dateiname = "auftrag.pdf",
            BlobPfad = "secret/auftrag.pdf",
            Auftragsnummer = "A-100",
            Kundennummer = "K-100",
            Maschinentyp = "RDM 76K",
            ErstelltAm = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var result = await CreateTools(db).SearchCustomersAsync("Muster", 10);

        var hit = Assert.Single(result.Customers);
        Assert.Equal(customer.Id, hit.Id);
        Assert.Equal(1, hit.Offers);
        Assert.Equal(1, hit.CustomerOrders);
        Assert.Equal(1, hit.MachineOrders);
        Assert.False(result.IsTruncated);
    }

    [Fact]
    public async Task GetCustomerContext_ReturnsStructuredFieldsWithoutDocumentSecrets()
    {
        await using var db = CreateDb();
        var customer = new Kunde
        {
            Name = "Beispiel GmbH",
            NormalisierterName = "BEISPIEL",
            NormalisierteAdresse = "",
            Status = KundeStatus.Bestaetigt,
            ErstelltAm = DateTime.UtcNow,
            AktualisiertAm = DateTime.UtcNow,
        };
        db.Kunden.Add(customer);
        await db.SaveChangesAsync();

        var offer = new Angebot
        {
            KundeId = customer.Id,
            Angebotsnummer = "AN-42",
            Version = 2,
            Liefertermin = "KW 40",
            Volltext = "VERTRAULICHER VOLLTEXT",
            Dateiname = "angebot.pdf",
            BlobPfad = "secret/angebot.pdf",
            HochgeladenAm = DateTime.UtcNow,
        };
        db.Angebote.Add(offer);
        await db.SaveChangesAsync();

        db.Auftragsbestaetigungen.Add(new Auftragsbestaetigung
        {
            KundeId = customer.Id,
            AngebotId = offer.Id,
            Nummer = "KB-42",
            Volltext = "VERTRAULICHER BESTELLTEXT",
            SonstigeAbweichungen = "Lieferbedingung abweichend",
            Dateiname = "bestellung.pdf",
            BlobPfad = "secret/bestellung.pdf",
            HochgeladenAm = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var result = await CreateTools(db).GetCustomerContextAsync(customer.Id);

        Assert.Equal("Beispiel GmbH", result.Customer.DisplayName);
        Assert.Equal("AN-42", Assert.Single(result.Offers).OfferNumber);
        var order = Assert.Single(result.CustomerOrders);
        Assert.Equal("KB-42", order.OrderNumber);
        Assert.Equal("Lieferbedingung abweichend", order.OtherDeviations);
        Assert.DoesNotContain("VOLLTEXT", System.Text.Json.JsonSerializer.Serialize(result));
        Assert.DoesNotContain("secret/", System.Text.Json.JsonSerializer.Serialize(result));
    }

    [Fact]
    public async Task SearchSupplierCommitments_ReturnsOnlyOpenOverduePositions()
    {
        await using var db = CreateDb();
        db.Lieferanten.Add(new Lieferant { Kreditor = 1000, Name = "Teile GmbH" });
        db.Dispositionspositionen.AddRange(
            new Dispositionsposition
            {
                Einkaufsbeleg = "450001",
                Position = "10",
                Schluessel = "450001-10",
                LieferantKreditor = 1000,
                Lieferdatum = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1),
                Material = "MAT-1",
                Kurztext = "Heizung",
                Bestellmenge = 10,
                NochZuLiefernMenge = 4,
                Nettopreis = 999,
                ImportiertAm = DateTime.UtcNow,
            },
            new Dispositionsposition
            {
                Einkaufsbeleg = "450002",
                Position = "20",
                Schluessel = "450002-20",
                LieferantKreditor = 1000,
                Lieferdatum = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-2),
                Kurztext = "Erledigt",
                Bestellmenge = 5,
                NochZuLiefernMenge = 0,
                Nettopreis = 123,
                ImportiertAm = DateTime.UtcNow,
            });
        await db.SaveChangesAsync();

        var result = await CreateTools(db).SearchSupplierCommitmentsAsync(
            query: "Heizung",
            overdueOnly: true,
            limit: 10);

        var commitment = Assert.Single(result.Commitments);
        Assert.Equal("450001", commitment.PurchaseDocument);
        Assert.Equal("Ueberfaellig", commitment.DueStatus);
        Assert.Equal(4, commitment.OutstandingQuantity);
        Assert.DoesNotContain("999", System.Text.Json.JsonSerializer.Serialize(result));
    }

    [Fact]
    public async Task SearchBillOfMaterials_RequiresAtLeastOneFilter()
    {
        await using var db = CreateDb();

        var exception = await Assert.ThrowsAsync<McpException>(() =>
            CreateTools(db).SearchBillOfMaterialsAsync());

        Assert.Contains("Mindestens", exception.Message);
    }
}
