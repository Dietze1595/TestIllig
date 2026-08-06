using Illig_AI_Platform.Shared.Data;
using Illig_AI_Platform.Shared.Lieferantenassistent;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace Illig_AI_Platform.Tests.Lieferantenassistent;

public class LieferantenassistentImportServiceTests : IDisposable
{
    private readonly List<string> _tempDateien = [];

    // Der Service umschließt die Importe mit einer Transaktion (echte Verhalten
    // gegen MySQL/Pomelo). Der InMemory-Provider unterstützt keine Transaktionen
    // und würde ohne diese Warnungsunterdrückung mit einer TransactionIgnoredWarning
    // abbrechen — die Transaktion wird dort einfach als No-Op behandelt.
    private static AppDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options);

    private string SchreibeTempDatei(string inhalt)
    {
        var pfad = Path.GetTempFileName();
        File.WriteAllText(pfad, inhalt);
        _tempDateien.Add(pfad);
        return pfad;
    }

    public void Dispose()
    {
        foreach (var pfad in _tempDateien)
            File.Delete(pfad);
        GC.SuppressFinalize(this);
    }

    private const string LieferantenCsv =
        "Kreditor;Land;Name 1;Ort;Postleitzahl;Straße;Adresse\n" +
        "1051;DE;R+W Antriebselemente GmbH;Klingenberg;63911;Ottostrasse 5;12345\n";

    private const string MailCsv =
        "Adressnummer;E-Mail-Adresse;Standard-Adr.\n" +
        "12345;bestellung@rw-antriebselemente.de;X\n";

    private const string DispoCsv =
        "Lieferant/Lieferwerk;Anzahl der Positionen;Einkäufergruppe;Einkaufsbeleg;Position;Belegdatum;Lieferdatum;Auftragsbestätigung;Material;Kurztext;Werk;Bestellmenge;Bestellmengeneinheit;Nettopreis;Währung;Preiseinheit;Einteilungsmenge;Gelieferte Menge;noch zu liefern (Menge);Menge in Lager-ME\n" +
        "1051       R+W Antriebselemente GmbH;1;021;45647126;10;10.07.2026;17.08.2026;;9152207;Kupplung_BKL_30_14_24;0001;2;ST;71,64;EUR;1;2;0;2;2\n";

    [Fact]
    public async Task ImportierenAsync_ImportiertAlle3DateienInDieDb()
    {
        await using var db = CreateDb();
        var service = new LieferantenassistentImportService(db, NullLogger<LieferantenassistentImportService>.Instance);

        var bericht = await service.ImportierenAsync(
            SchreibeTempDatei(DispoCsv), SchreibeTempDatei(LieferantenCsv), SchreibeTempDatei(MailCsv));

        Assert.Equal(1, bericht.LieferantenGesamt);
        Assert.Equal(1, bericht.EmailAdressenGesamt);
        Assert.Equal(1, bericht.DispositionspositionenGesamt);
        Assert.Single(db.Lieferanten);
        Assert.Single(db.LieferantEmailAdressen);
        Assert.Single(db.Dispositionspositionen);
    }

    [Fact]
    public async Task ImportierenAsync_ErsetztBeiErneutemImportStattZuDuplizieren()
    {
        await using var db = CreateDb();
        var service = new LieferantenassistentImportService(db, NullLogger<LieferantenassistentImportService>.Instance);
        await service.ImportierenAsync(
            SchreibeTempDatei(DispoCsv), SchreibeTempDatei(LieferantenCsv), SchreibeTempDatei(MailCsv));

        var bericht = await service.ImportierenAsync(
            SchreibeTempDatei(DispoCsv), SchreibeTempDatei(LieferantenCsv), SchreibeTempDatei(MailCsv));

        Assert.Equal(1, bericht.LieferantenErsetzt);
        Assert.Equal(1, bericht.EmailAdressenErsetzt);
        Assert.Equal(1, bericht.DispositionspositionenErsetzt);
        Assert.Single(db.Lieferanten);
        Assert.Single(db.LieferantEmailAdressen);
        Assert.Single(db.Dispositionspositionen);
    }

    [Fact]
    public async Task ImportierenAsync_ErsetztFeldwerteBeiErneutemImportStattSieZuUebernehmen()
    {
        const string lieferantenCsvAktualisiert =
            "Kreditor;Land;Name 1;Ort;Postleitzahl;Straße;Adresse\n" +
            "1051;DE;R+W Antriebselemente GmbH (aktualisiert);Klingenberg;63911;Ottostrasse 5;12345\n";

        await using var db = CreateDb();
        var service = new LieferantenassistentImportService(db, NullLogger<LieferantenassistentImportService>.Instance);
        await service.ImportierenAsync(
            SchreibeTempDatei(DispoCsv), SchreibeTempDatei(LieferantenCsv), SchreibeTempDatei(MailCsv));

        await service.ImportierenAsync(
            SchreibeTempDatei(DispoCsv), SchreibeTempDatei(lieferantenCsvAktualisiert), SchreibeTempDatei(MailCsv));

        Assert.Equal("R+W Antriebselemente GmbH (aktualisiert)", db.Lieferanten.Single().Name);
    }

    [Fact]
    public async Task ImportierenAsync_ImportiertMehrereEinteilungenDerselbenPositionOhneKollision()
    {
        // Regressionstest für einen realen Datenfall: eine Bestellmenge wird auf zwei
        // Lieferplan-Einteilungen mit unterschiedlichem Lieferdatum aufgeteilt — beide Zeilen
        // teilen sich Einkaufsbeleg+Position und dürfen sich beim Import nicht überschreiben
        // (führte vor dem Fix zu einer Duplicate-Key-Exception gegen die echte MySQL-DB).
        const string dispoCsvMitZweiEinteilungen =
            "Lieferant/Lieferwerk;Anzahl der Positionen;Einkäufergruppe;Einkaufsbeleg;Position;Belegdatum;Lieferdatum;Auftragsbestätigung;Material;Kurztext;Werk;Bestellmenge;Bestellmengeneinheit;Nettopreis;Währung;Preiseinheit;Einteilungsmenge;Gelieferte Menge;noch zu liefern (Menge);Menge in Lager-ME\n" +
            "1051       R+W Antriebselemente GmbH;1;021;45645541;20;06.05.2026;14.07.2026;;9228805;Verteiler_aussen_FT;0001;24;ST;320,95;EUR;1;12;0;12;24\n" +
            "1051       R+W Antriebselemente GmbH;1;021;45645541;20;08.07.2026;20.07.2026;;9228805;Verteiler_aussen_FT;0001;24;ST;320,95;EUR;1;12;0;12;24\n";

        await using var db = CreateDb();
        var service = new LieferantenassistentImportService(db, NullLogger<LieferantenassistentImportService>.Instance);

        var bericht = await service.ImportierenAsync(
            SchreibeTempDatei(dispoCsvMitZweiEinteilungen), SchreibeTempDatei(LieferantenCsv), SchreibeTempDatei(MailCsv));

        Assert.Equal(2, bericht.DispositionspositionenGesamt);
        Assert.Equal(2, db.Dispositionspositionen.Count());
    }
}
