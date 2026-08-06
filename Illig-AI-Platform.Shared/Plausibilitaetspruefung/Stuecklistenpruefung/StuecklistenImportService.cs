using Illig_AI_Platform.Shared.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Illig_AI_Platform.Shared.Plausibilitaetspruefung.Stuecklistenpruefung;

/// <summary>
/// Merged den Maximalstückliste-Baum (<see cref="MaximalstuecklisteTxtParser"/>) mit den
/// Umsetzungsmatrix-Bedingungen (<see cref="UmsetzungsmatrixXlsxParser"/>) zu
/// <see cref="MaximalstuecklistenPosition"/>-Entitäten. Full-Replace pro
/// <c>MaschinentypSchluessel</c>. Matrix-Zeilen, deren Hierarchiepfad keinen passenden Knoten
/// im Baum findet, werden protokolliert statt stillschweigend verworfen oder den Import
/// abzubrechen — bei den echten Beispieldateien war das bei 16 von 239 Zeilen der Fall
/// (Versions-Drift zwischen den beiden Quelldateien, siehe Design-Spec).
/// </summary>
public class StuecklistenImportService(AppDbContext db, ILogger<StuecklistenImportService> logger)
{
    public async Task ImportierenAsync(
        string maschinentypSchluessel, string kopfmaterial, RohPosition wurzel, List<MatrixZeile> matrixZeilen)
    {
        // Full-Replace + Neuaufbau ist mehrschrittig (Löschen der alten Stückliste, Anlegen der
        // neuen, rekursives Einfügen der Positionen) — eine Transaktion, damit ein Fehler mitten
        // im Baumaufbau nicht eine gelöschte alte und eine unvollständige neue Stückliste zurücklässt.
        await using var transaction = await db.Database.BeginTransactionAsync();

        var vorhandene = await db.MaschinentypStuecklisten
            .SingleOrDefaultAsync(m => m.MaschinentypSchluessel == maschinentypSchluessel);
        if (vorhandene is not null)
        {
            db.MaximalstuecklistenPositionen.RemoveRange(
                db.MaximalstuecklistenPositionen.Where(p => p.MaschinentypStuecklisteId == vorhandene.Id));
            db.MaschinentypStuecklisten.Remove(vorhandene);
            await db.SaveChangesAsync();
        }

        var stueckliste = new MaschinentypStueckliste
        {
            BomTyp = "MANUAL_IMPORT",
            MaschinentypSchluessel = maschinentypSchluessel,
            Kopfmaterial = kopfmaterial,
            Beschreibung = maschinentypSchluessel,
            GueltigAm = DateOnly.FromDateTime(DateTime.UtcNow),
            ImportiertAm = DateTime.UtcNow
        };
        db.MaschinentypStuecklisten.Add(stueckliste);
        await db.SaveChangesAsync();

        // Derselbe Pfad kann in der echten Matrix mehrfach mit unterschiedlichen Bedingungen
        // auftauchen (verifiziert an der echten Datei, z. B. "9237831" auf zwei Zeilen) — dann
        // reicht irgendeine der Bedingungen zur Aufnahme, daher ODER-Verknüpfung statt Kollision.
        var bedingungenNachPfad = matrixZeilen
            .GroupBy(z => string.Join('/', z.Pfad))
            .ToDictionary(g => g.Key, g => g.Count() == 1 ? g.First().Bedingung : string.Join(" / ", g.Select(z => $"({z.Bedingung})")));
        var zugeordnetePfade = new HashSet<string>();

        EinfuegenRekursiv(wurzel, [], null, stueckliste.Id, bedingungenNachPfad, zugeordnetePfade, 0);
        await db.SaveChangesAsync();

        await transaction.CommitAsync();

        foreach (var zeile in matrixZeilen)
        {
            var pfad = string.Join('/', zeile.Pfad);
            if (!zugeordnetePfade.Contains(pfad))
            {
                logger.LogWarning(
                    "Umsetzungsmatrix-Bedingung ohne passende Position in der Maximalstückliste " +
                    "(Maschinentyp {MaschinentypSchluessel}): Pfad {Pfad}, Bedingung {Bedingung}.",
                    maschinentypSchluessel, pfad, zeile.Bedingung);
            }
        }
    }

    private void EinfuegenRekursiv(
        RohPosition knoten, List<string> pfadBisher, int? parentId, int stuecklisteId,
        Dictionary<string, string> bedingungenNachPfad, HashSet<string> zugeordnetePfade, int reihenfolge)
    {
        var pfad = new List<string>(pfadBisher) { knoten.Artikelnummer };
        var pfadSchluessel = string.Join('/', pfad);
        var bedingung = bedingungenNachPfad.GetValueOrDefault(pfadSchluessel);
        if (bedingung is not null)
            zugeordnetePfade.Add(pfadSchluessel);

        var position = new MaximalstuecklistenPosition
        {
            MaschinentypStuecklisteId = stuecklisteId,
            ParentId = parentId,
            Reihenfolge = reihenfolge,
            SapNodeId = Guid.NewGuid().ToString("N"),
            Typ = "MANUAL_IMPORT",
            Artikelnummer = knoten.Artikelnummer,
            Bezeichnung = knoten.Bezeichnung,
            Menge = knoten.Menge,
            Einheit = knoten.Einheit,
            Bedingung = bedingung
        };
        db.MaximalstuecklistenPositionen.Add(position);
        db.SaveChanges(); // Id wird für die Kinder als ParentId gebraucht.

        for (var i = 0; i < knoten.Kinder.Count; i++)
            EinfuegenRekursiv(knoten.Kinder[i], pfad, position.Id, stuecklisteId, bedingungenNachPfad, zugeordnetePfade, i);
    }
}
