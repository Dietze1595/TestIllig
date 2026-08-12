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
    private readonly record struct PfadVorkommenSchluessel(string Pfad, int Vorkommen);

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

        // RDM75 liefert für doppelte Pfade eine Vorkommensnummer, damit jede Matrixzeile genau
        // ihrem Baumknoten zugeordnet wird. Parser ohne Vorkommensnummer behalten die bisherige
        // pfadbasierte ODER-Verknüpfung und damit ihr bestehendes Importverhalten.
        var bedingungenNachPfad = matrixZeilen
            .Where(z => z.PfadVorkommen is null)
            .GroupBy(z => string.Join('/', z.Pfad))
            .ToDictionary(g => g.Key, VerbindeBedingungen);
        var bedingungenNachPfadVorkommen = matrixZeilen
            .Where(z => z.PfadVorkommen is not null)
            .GroupBy(z => new PfadVorkommenSchluessel(
                string.Join('/', z.Pfad),
                z.PfadVorkommen!.Value))
            .ToDictionary(g => g.Key, VerbindeBedingungen);
        var zugeordnetePfade = new HashSet<string>();
        var zugeordnetePfadVorkommen = new HashSet<PfadVorkommenSchluessel>();
        var naechstesVorkommenNachPfad = new Dictionary<string, int>();

        EinfuegenRekursiv(
            wurzel,
            [],
            null,
            stueckliste.Id,
            bedingungenNachPfad,
            bedingungenNachPfadVorkommen,
            zugeordnetePfade,
            zugeordnetePfadVorkommen,
            naechstesVorkommenNachPfad,
            0);
        await db.SaveChangesAsync();

        await transaction.CommitAsync();

        foreach (var zeile in matrixZeilen)
        {
            var pfad = string.Join('/', zeile.Pfad);
            var wurdeZugeordnet = zeile.PfadVorkommen is int vorkommen
                ? zugeordnetePfadVorkommen.Contains(new PfadVorkommenSchluessel(pfad, vorkommen))
                : zugeordnetePfade.Contains(pfad);
            if (!wurdeZugeordnet)
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
        Dictionary<string, string> bedingungenNachPfad,
        Dictionary<PfadVorkommenSchluessel, string> bedingungenNachPfadVorkommen,
        HashSet<string> zugeordnetePfade,
        HashSet<PfadVorkommenSchluessel> zugeordnetePfadVorkommen,
        Dictionary<string, int> naechstesVorkommenNachPfad,
        int reihenfolge)
    {
        var pfad = new List<string>(pfadBisher) { knoten.Artikelnummer };
        var pfadSchluessel = string.Join('/', pfad);
        var vorkommen = naechstesVorkommenNachPfad.GetValueOrDefault(pfadSchluessel);
        naechstesVorkommenNachPfad[pfadSchluessel] = vorkommen + 1;
        var pfadVorkommenSchluessel = new PfadVorkommenSchluessel(pfadSchluessel, vorkommen);
        var bedingungNachVorkommen = bedingungenNachPfadVorkommen.GetValueOrDefault(pfadVorkommenSchluessel);
        var bedingungNachPfad = bedingungenNachPfad.GetValueOrDefault(pfadSchluessel);
        var bedingung = bedingungNachVorkommen ?? bedingungNachPfad;
        if (bedingungNachVorkommen is not null)
            zugeordnetePfadVorkommen.Add(pfadVorkommenSchluessel);
        else if (bedingungNachPfad is not null)
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
            EinfuegenRekursiv(
                knoten.Kinder[i],
                pfad,
                position.Id,
                stuecklisteId,
                bedingungenNachPfad,
                bedingungenNachPfadVorkommen,
                zugeordnetePfade,
                zugeordnetePfadVorkommen,
                naechstesVorkommenNachPfad,
                i);
    }

    private static string VerbindeBedingungen(IEnumerable<MatrixZeile> zeilen)
    {
        var liste = zeilen.ToList();
        return liste.Count == 1
            ? liste[0].Bedingung
            : string.Join(" / ", liste.Select(z => $"({z.Bedingung})"));
    }
}
