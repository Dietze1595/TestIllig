using Illig_AI_Platform.Shared.Plausibilitaetspruefung;
using Illig_AI_Platform.Shared.Data;
using Illig_AI_Platform.Shared.Plausibilitaetspruefung.Stuecklistenpruefung;
using Microsoft.EntityFrameworkCore;

namespace Illig_AI_Platform.Shared.Plausibilitaetspruefung.Sondermerkmalsuche;

/// <summary>
/// Echte <see cref="ISondermerkmalService"/>-Implementierung gegen die Stücklistenprüfung-
/// Verlaufsdaten. Ersetzt <c>MockSondermerkmalService</c>. Bewusst NICHT auf
/// <c>UserProfileId</c> gefiltert — anders als die "meine Historie"-Ansicht vergleicht die
/// Sondermerkmalsuche firmenweit über alle Mitarbeiter-Uploads hinweg.
/// </summary>
public sealed class DatenbankSondermerkmalService(
    AppDbContext db,
    IBlobStorageService? blobStorage = null) : ISondermerkmalService
{
    private static string Normalize(string s) => s.Trim();

    // Die Auftragsinformation erfasst die Auftragsnummer oft als "<Nummer> / <Linie>"
    // (z. B. "11055627 / 40"): ein Auftrag kann mehrere Linien haben. Gibt der Nutzer nur die
    // reine Nummer ein, werden die Sonderoptionen aller Linien vereinigt; gibt er zusätzlich
    // eine Linie an ("11055627 / 40"), zählen nur die Sonderoptionen dieser Linie.
    // Am Schrägstrich (mit oder ohne umgebende Leerzeichen) wird in Basis + optionale Linie zerlegt.
    private static (string Basis, string? Linie) ZerlegeAuftragsnummer(string auftragsnummer)
    {
        var teile = auftragsnummer.Split('/', 2);
        var basis = teile[0].Trim();
        var linie = teile.Length > 1 ? teile[1].Trim() : null;
        return (basis, string.IsNullOrWhiteSpace(linie) ? null : linie);
    }

    private static string BasisAuftragsnummer(string auftragsnummer) =>
        ZerlegeAuftragsnummer(auftragsnummer).Basis;

    private static string? LinieVon(string auftragsnummer) =>
        ZerlegeAuftragsnummer(auftragsnummer).Linie;

    private sealed record VereinigterAuftrag(
        string Auftragsnummer, string Maschinentyp, DateOnly Datum, string Kundennummer, string? Kundenname,
        IReadOnlyList<(string Merkmalsnummer, string Beschreibung, string Position, string Quelle)> Sonderoptionen);

    private async Task<VereinigterAuftrag?> LadeUndVereinigeAsync(string auftragsnummer)
    {
        var (basis, linie) = ZerlegeAuftragsnummer(Normalize(auftragsnummer));
        var eintraege = (await db.StuecklistenpruefungVerlaufEintraege
                .Where(e => e.Auftragsnummer.StartsWith(basis))
                .ToListAsync())
            .Where(e => BasisAuftragsnummer(e.Auftragsnummer) == basis)
            // Ist eine Linie angegeben, werden nur deren Einträge berücksichtigt; sonst alle Linien.
            .Where(e => linie is null
                || string.Equals(LinieVon(e.Auftragsnummer), linie, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (eintraege.Count == 0)
            return null;

        var eintragIds = eintraege.Select(e => e.Id).ToList();
        var merkmale = await db.VerlaufMerkmale
            .Where(m => eintragIds.Contains(m.VerlaufEintragId) && m.Kategorie == MerkmalKategorie.Sonderoption)
            .ToListAsync();
        var eintragNachId = eintraege.ToDictionary(e => e.Id);

        // Bei mehreren Uploads derselben Auftragsnummer vereinigen wir die Sonderoptionen;
        // gibt es dieselbe Merkmalsnummer mehrfach, gewinnt der zuletzt erstellte Verlauf-Eintrag.
        var vereinigteSonderoptionen = merkmale
            .GroupBy(m => m.Merkmalsnummer, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.OrderByDescending(m => eintragNachId[m.VerlaufEintragId].ErstelltAm).First())
            .Select(m => (m.Merkmalsnummer, m.Beschreibung, m.Position, Quelle: eintragNachId[m.VerlaufEintragId].Dateiname))
            .ToList();

        var neuester = eintraege.OrderByDescending(e => e.ErstelltAm).First();
        var datum = neuester.Datum ?? DateOnly.FromDateTime(neuester.ErstelltAm);
        // Ohne Linie zeigen wir die reine Auftragsnummer, mit Linie die konkrete "<Nummer> / <Linie>".
        var anzeigeAuftragsnummer = linie is null ? basis : $"{basis} / {linie}";

        return new VereinigterAuftrag(
            anzeigeAuftragsnummer, neuester.Maschinentyp, datum, neuester.Kundennummer, neuester.Kundenname, vereinigteSonderoptionen);
    }

    public async Task<StuecklisteAnalyse?> AnalyzeAsync(string auftragsnummer)
    {
        var auftrag = await LadeUndVereinigeAsync(auftragsnummer);
        if (auftrag is null)
            return null;

        var sondermerkmale = auftrag.Sonderoptionen
            .Select(s => new Sondermerkmal(s.Position, s.Merkmalsnummer, s.Beschreibung, s.Quelle))
            .ToList();

        return new StuecklisteAnalyse(
            auftrag.Auftragsnummer, auftrag.Maschinentyp, auftrag.Datum,
            auftrag.Kundennummer, auftrag.Kundenname, sondermerkmale);
    }

    public async Task<StuecklisteDetail?> GetDetailAsync(string auftragsnummer)
    {
        var auftrag = await LadeUndVereinigeAsync(auftragsnummer);
        if (auftrag is null)
            return null;

        var merkmale = auftrag.Sonderoptionen
            .Select(s => new StuecklisteDetailMerkmal(s.Merkmalsnummer, s.Beschreibung, auftrag.Auftragsnummer, s.Quelle))
            .ToList();

        return new StuecklisteDetail(
            auftrag.Auftragsnummer,
            auftrag.Maschinentyp,
            auftrag.Datum,
            auftrag.Kundenname,
            merkmale);
    }

    public async Task<ReferenzDokument?> GetDokumentAsync(
        string auftragsnummer,
        CancellationToken cancellationToken = default)
    {
        var key = BasisAuftragsnummer(Normalize(auftragsnummer));
        var eintraege = (await db.StuecklistenpruefungVerlaufEintraege
                .AsNoTracking()
                .Where(e => e.Auftragsnummer.StartsWith(key))
                .ToListAsync(cancellationToken))
            .Where(e => BasisAuftragsnummer(e.Auftragsnummer) == key)
            .OrderByDescending(e => e.ErstelltAm)
            .ToList();
        var neuester = eintraege.FirstOrDefault();
        if (neuester is null)
            return null;

        if (blobStorage is null)
            throw new InvalidOperationException("BlobStorage ist für Referenzdokumente nicht konfiguriert.");

        var inhalt = await blobStorage.OpenReadAsync(neuester.BlobPfad, cancellationToken);
        return new ReferenzDokument(neuester.Dateiname, inhalt);
    }

    public async Task<IReadOnlyList<ReferenzTreffer>> SearchAsync(SearchRequest request)
    {
        var eintraege = await db.StuecklistenpruefungVerlaufEintraege.ToListAsync();
        var merkmale = await db.VerlaufMerkmale
            .Where(m => m.Kategorie == MerkmalKategorie.Sonderoption)
            .ToListAsync();
        var merkmaleNachEintragId = merkmale.ToLookup(m => m.VerlaufEintragId);

        // Ganze Tabelle im Speicher gruppieren statt per SQL-GroupBy: die Menge bleibt
        // überschaubar, und die "neuester Eintrag gewinnt"-Merge-Regel ist in LINQ-to-Objects
        // einfacher auszudrücken als in SQL.
        var kandidaten = eintraege
            .GroupBy(e => BasisAuftragsnummer(e.Auftragsnummer), StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var neuester = g.OrderByDescending(e => e.ErstelltAm).First();
                var merkmalsnummern = g
                    .SelectMany(e => merkmaleNachEintragId[e.Id])
                    .Select(m => m.Merkmalsnummer)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                var datum = neuester.Datum ?? DateOnly.FromDateTime(neuester.ErstelltAm);
                return new StuecklisteKandidat(
                    g.Key, neuester.Maschinentyp, datum, merkmalsnummern,
                    neuester.Kundennummer, neuester.Kundenname);
            })
            .ToList();

        var ausschlussAuftragsnummer = string.IsNullOrWhiteSpace(request.QuellAuftragsnummer)
            ? null
            : BasisAuftragsnummer(Normalize(request.QuellAuftragsnummer));
        var gesuchteMerkmale = new HashSet<string>(
            request.Merkmalsnummern,
            StringComparer.OrdinalIgnoreCase);

        // Vor der Top-5-Begrenzung global bestimmen, in welchem Referenzauftrag jedes
        // gesuchte Merkmal zuletzt vorkam. Der Quellauftrag selbst zählt nicht als Referenz.
        var aktuellsterAuftragJeMerkmal = kandidaten
            .Where(k => !string.Equals(
                k.Auftragsnummer,
                ausschlussAuftragsnummer,
                StringComparison.OrdinalIgnoreCase))
            .SelectMany(k => k.Merkmalsnummern
                .Where(gesuchteMerkmale.Contains)
                .Select(m => new { Merkmal = m, Kandidat = k }))
            .GroupBy(x => x.Merkmal, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(x => x.Kandidat.Abschlussdatum)
                    .ThenByDescending(x => x.Kandidat.Auftragsnummer, StringComparer.OrdinalIgnoreCase)
                    .First().Kandidat.Auftragsnummer,
                StringComparer.OrdinalIgnoreCase);

        // Ohne Quellauftragsnummer ist das eine direkte Merkmalsnummer-Suche (Nutzer sucht
        // "wer hat dieses eine Merkmal") — dort ergibt eine Top-5-Begrenzung keinen Sinn, da
        // jeder Treffer ohnehin nur 1/1 oder 0/1 sein kann (kein sinnvolles Ranking).
        var top = string.IsNullOrWhiteSpace(request.QuellAuftragsnummer) ? int.MaxValue : 5;
        var treffer = SondermerkmalMatcher.Rank(
            request.Merkmalsnummern, kandidaten,
            ausschlussAuftragsnummer,
            top);

        return treffer
            .Select(t => t with
            {
                AktuellsteMerkmalsnummern = t.UeberschneidendeMerkmalsnummern
                    .Where(m => aktuellsterAuftragJeMerkmal.TryGetValue(m, out var auftragsnummer)
                                && string.Equals(
                                    auftragsnummer,
                                    t.Auftragsnummer,
                                    StringComparison.OrdinalIgnoreCase))
                    .ToList()
            })
            .ToList();
    }
}
