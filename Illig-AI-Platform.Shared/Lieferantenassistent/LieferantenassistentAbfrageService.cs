using Illig_AI_Platform.Shared.Data;
using Microsoft.EntityFrameworkCore;

namespace Illig_AI_Platform.Shared.Lieferantenassistent;

/// <summary>
/// Liefert die offenen Dispositionspositionen (noch zu liefernde Menge &gt; 0) gejoint mit
/// Lieferant und E-Mail-Adressen, inkl. Ampel-Status. <paramref name="heute"/> ist optional
/// injizierbar, damit Tests unabhängig vom Systemdatum deterministisch bleiben.
/// <paramref name="status"/> und <paramref name="einkaeufergruppen"/> sind Mehrfachauswahl-
/// Filter (ODER-Verknüpfung innerhalb der jeweiligen Kategorie, leer/null = kein Filter).
/// Die beiden Kategorien sowie <paramref name="suche"/> werden untereinander UND-verknüpft.
/// </summary>
public class LieferantenassistentAbfrageService(AppDbContext db)
{
    public async Task<IReadOnlyList<OffenePositionAnsicht>> OffenePositionenAsync(
        string? suche, IReadOnlyList<LieferterminStatus>? status = null, DateOnly? heute = null,
        IReadOnlyList<string>? einkaeufergruppen = null)
    {
        var heuteWert = heute ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var query =
            from p in db.Dispositionspositionen
            where p.NochZuLiefernMenge > 0
            join l in db.Lieferanten on p.LieferantKreditor equals l.Kreditor
            select new { Position = p, Lieferant = l };

        // Case-insensitive Suche wird bewusst nicht in C# erzwungen (z.B. via ToUpper/StringComparison),
        // sondern kommt aus der Standard-Collation der MySQL-DB (case-insensitiv). Bei einer Schema-
        // oder DB-Änderung (z.B. case-sensitive Collation) würde dieses Verhalten sonst stillschweigend kippen.
        if (!string.IsNullOrWhiteSpace(suche))
        {
            var suchbegriff = suche.Trim();
            query = query.Where(x =>
                x.Lieferant.Name.Contains(suchbegriff) ||
                (x.Position.Material != null && x.Position.Material.Contains(suchbegriff)) ||
                x.Position.Kurztext.Contains(suchbegriff) ||
                x.Position.Einkaufsbeleg.Contains(suchbegriff));
        }

        if (einkaeufergruppen is { Count: > 0 })
            query = query.Where(x => einkaeufergruppen.Contains(x.Position.Einkaeufergruppe));

        var geladen = await query.AsNoTracking().ToListAsync();

        var adressNummern = geladen
            .Select(x => x.Lieferant.AdressNummer)
            .Where(a => a.HasValue)
            .Select(a => a!.Value)
            .Distinct()
            .ToList();
        var emailsNachAdressNummer = (await db.LieferantEmailAdressen
            .AsNoTracking()
            .Where(e => adressNummern.Contains(e.AdressNummer))
            .ToListAsync())
            .ToLookup(e => e.AdressNummer);

        return geladen
            .Select(x => new OffenePositionAnsicht(
                x.Position.Id,
                x.Position.Einkaufsbeleg,
                x.Position.Position,
                x.Lieferant.Kreditor,
                x.Lieferant.Name,
                x.Position.Einkaeufergruppe,
                x.Position.Material,
                x.Position.Kurztext,
                x.Position.Bestellmenge,
                x.Position.NochZuLiefernMenge,
                x.Position.Lieferdatum,
                LieferterminStatusBerechnung.Berechnen(x.Position.Lieferdatum, heuteWert),
                (x.Lieferant.AdressNummer is null ? [] : emailsNachAdressNummer[x.Lieferant.AdressNummer.Value])
                    .Select(e => new LieferantEmailAdresseAnsicht(e.EmailAdresse, e.IstStandard))
                    .ToList()))
            .Where(a => status is null || status.Count == 0 || status.Contains(a.Status))
            .OrderBy(a => a.Lieferdatum)
            .ToList();
    }

    /// <summary>
    /// Distinct vorkommende Einkäufergruppen aller offenen Positionen, unabhängig vom aktuell
    /// gesetzten Filter — damit im Dropdown jederzeit zu jeder Gruppe gewechselt werden kann.
    /// </summary>
    public async Task<IReadOnlyList<string>> EinkaeufergruppenAsync()
    {
        return await db.Dispositionspositionen
            .AsNoTracking()
            .Where(p => p.NochZuLiefernMenge > 0 && p.Einkaeufergruppe != null)
            .Select(p => p.Einkaeufergruppe!)
            .Distinct()
            .OrderBy(g => g)
            .ToListAsync();
    }
}
