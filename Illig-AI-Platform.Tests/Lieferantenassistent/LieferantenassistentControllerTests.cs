using System.Reflection;
using Illig_AI_Platform.Controllers.Lieferantenassistent;
using Illig_AI_Platform.Shared.Data;
using Illig_AI_Platform.Shared.Lieferantenassistent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Illig_AI_Platform.Tests.Lieferantenassistent;

public class LieferantenassistentControllerTests
{
    // ConfigureWarnings-Unterdrückung wie in LieferantenassistentImportServiceTests: Der
    // Import-Service umschließt die Importe mit einer Transaktion (echtes Verhalten gegen
    // MySQL/Pomelo); der InMemory-Provider unterstützt keine Transaktionen und würde ohne
    // diese Unterdrückung mit einer TransactionIgnoredWarning abbrechen.
    private static AppDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options);

    private static LieferantenassistentController CreateController(AppDbContext db) =>
        new(
            new LieferantenassistentAbfrageService(db),
            new LieferantenassistentImportService(db, NullLogger<LieferantenassistentImportService>.Instance),
            NullLogger<LieferantenassistentController>.Instance);

    [Fact]
    public void KlasseErfordertLieferantenassistentRolle_ImportErfordertZusaetzlichAdmin()
    {
        var klasse = typeof(LieferantenassistentController).GetCustomAttribute<AuthorizeAttribute>();
        var importMethode = typeof(LieferantenassistentController).GetMethod(nameof(LieferantenassistentController.Import))!;

        Assert.Equal(AppRoles.Lieferantenassistent, klasse!.Roles);
        Assert.Equal(AppRoles.Admin, importMethode.GetCustomAttribute<AuthorizeAttribute>()!.Roles);
    }

    [Fact]
    public async Task Positionen_GibtOkMitLeererListe_WennKeineOffenenPositionenVorhanden()
    {
        await using var db = CreateDb();
        var controller = CreateController(db);

        var result = await controller.Positionen(null, null, null);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var liste = Assert.IsAssignableFrom<IReadOnlyList<OffenePositionAnsicht>>(ok.Value);
        Assert.Empty(liste);
    }

    [Fact]
    public async Task Import_ImportiertUndGibtBerichtZurueck()
    {
        await using var db = CreateDb();
        var controller = CreateController(db);
        var lieferantenPfad = Path.GetTempFileName();
        var mailPfad = Path.GetTempFileName();
        var dispoPfad = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(lieferantenPfad,
                "Kreditor;Land;Name 1;Ort;Postleitzahl;Straße;Adresse\n1051;DE;R+W Antriebselemente GmbH;Klingenberg;63911;Ottostrasse 5;12345\n");
            await File.WriteAllTextAsync(mailPfad,
                "Adressnummer;E-Mail-Adresse;Standard-Adr.\n12345;bestellung@rw-antriebselemente.de;X\n");
            await File.WriteAllTextAsync(dispoPfad,
                "Lieferant/Lieferwerk;Anzahl der Positionen;Einkäufergruppe;Einkaufsbeleg;Position;Belegdatum;Lieferdatum;Auftragsbestätigung;Material;Kurztext;Werk;Bestellmenge;Bestellmengeneinheit;Nettopreis;Währung;Preiseinheit;Einteilungsmenge;Gelieferte Menge;noch zu liefern (Menge);Menge in Lager-ME\n" +
                "1051       R+W Antriebselemente GmbH;1;021;45647126;10;10.07.2026;17.08.2026;;9152207;Kupplung_BKL_30_14_24;0001;2;ST;71,64;EUR;1;2;0;2;2\n");

            var result = await controller.Import(new LieferantenassistentImportAnfrage(dispoPfad, lieferantenPfad, mailPfad));

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var bericht = Assert.IsType<LieferantenassistentImportBericht>(ok.Value);
            Assert.Equal(1, bericht.LieferantenGesamt);
            Assert.Equal(1, bericht.DispositionspositionenGesamt);
        }
        finally
        {
            File.Delete(lieferantenPfad);
            File.Delete(mailPfad);
            File.Delete(dispoPfad);
        }
    }

    [Fact]
    public async Task Einkaeufergruppen_GibtOkMitLeererListe_WennKeineOffenenPositionenVorhanden()
    {
        await using var db = CreateDb();
        var controller = CreateController(db);

        var result = await controller.Einkaeufergruppen();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var liste = Assert.IsAssignableFrom<IReadOnlyList<string>>(ok.Value);
        Assert.Empty(liste);
    }
}
