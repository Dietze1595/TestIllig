using Illig_AI_Platform.Shared.Data;
using Illig_AI_Platform.Shared.Services;
using Microsoft.Extensions.Logging;

namespace Illig_AI_Platform.Shared.Lieferantenassistent;

/// <summary>
/// Einmaliger Prototyp-Import der 3 CSV-Exporte (Dispositionsliste, LFA1-Stammdaten,
/// Mail-Adressen) von Server-Dateipfaden. Additiver Import pro fachlichem Schlüssel
/// (siehe <see cref="AdditiverImport"/>) — kein globales Löschen. Wird später durch den
/// SAP-Push über die bestehenden Staging-Endpunkte abgelöst (siehe Design-Spec).
/// </summary>
public class LieferantenassistentImportService(AppDbContext db, ILogger<LieferantenassistentImportService> logger)
{
    public async Task<LieferantenassistentImportBericht> ImportierenAsync(
        string dispositionslisteCsvPfad, string lieferantenstammdatenCsvPfad, string mailAdressenCsvPfad)
    {
        var lieferanten = LieferantenstammdatenCsvParser.Parse(await File.ReadAllTextAsync(lieferantenstammdatenCsvPfad));
        var emailAdressen = LieferantEmailAdresseCsvParser.Parse(await File.ReadAllTextAsync(mailAdressenCsvPfad));
        var positionen = DispositionslisteCsvParser.Parse(await File.ReadAllTextAsync(dispositionslisteCsvPfad));

        await using var transaction = await db.Database.BeginTransactionAsync();

        var lieferantenErsetzt = await AdditiverImport.ErsetzeProSchluesselAsync(db, lieferanten, l => l.Kreditor);
        var emailAdressenErsetzt = await AdditiverImport.ErsetzeProSchluesselAsync(db, emailAdressen, e => e.AdressNummer);
        var positionenErsetzt = await AdditiverImport.ErsetzeProSchluesselAsync(db, positionen, p => p.Schluessel);

        await transaction.CommitAsync();

        logger.LogInformation(
            "Lieferantenassistent-Import abgeschlossen: {LieferantenGelesen} Lieferanten ({LieferantenErsetzt} ersetzt), " +
            "{EmailsGelesen} E-Mail-Adressen ({EmailsErsetzt} ersetzt), {PositionenGelesen} Dispositionspositionen " +
            "({PositionenErsetzt} ersetzt).",
            lieferanten.Count, lieferantenErsetzt, emailAdressen.Count, emailAdressenErsetzt,
            positionen.Count, positionenErsetzt);

        return new LieferantenassistentImportBericht(
            lieferanten.Count, lieferantenErsetzt,
            emailAdressen.Count, emailAdressenErsetzt,
            positionen.Count, positionenErsetzt);
    }
}
