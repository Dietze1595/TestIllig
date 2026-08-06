namespace Illig_AI_Platform.Shared.Plausibilitaetspruefung.Sondermerkmalsuche;

/// <summary>
/// Reine, deterministische Ranking-Logik der Sondermerkmalsuche.
/// Zählt überschneidende Merkmalsnummern, sortiert absteigend nach Trefferzahl
/// und bei Gleichstand nach neuestem Abschlussdatum, begrenzt auf die Top N.
/// </summary>
public static class SondermerkmalMatcher
{
    public static IReadOnlyList<ReferenzTreffer> Rank(
        IReadOnlyList<string> eingabeMerkmalsnummern,
        IEnumerable<StuecklisteKandidat> kandidaten,
        string? ausschlussAuftragsnummer = null,
        int top = 5)
    {
        var eingabe = new HashSet<string>(eingabeMerkmalsnummern, StringComparer.OrdinalIgnoreCase);
        var gesamt = eingabe.Count;

        return kandidaten
            .Where(k => !string.Equals(k.Auftragsnummer, ausschlussAuftragsnummer, StringComparison.OrdinalIgnoreCase))
            .Select(k =>
            {
                var overlap = k.Merkmalsnummern
                    .Where(eingabe.Contains)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                return new ReferenzTreffer(
                    k.Auftragsnummer,
                    k.Maschinentyp,
                    k.Abschlussdatum,
                    overlap,
                    overlap.Count,
                    gesamt,
                    [],
                    k.Kundennummer,
                    k.Kundenname);
            })
            .Where(t => t.TrefferAnzahl > 0)
            .OrderByDescending(t => t.TrefferAnzahl)
            .ThenByDescending(t => t.Abschlussdatum)
            .Take(top)
            .ToList();
    }
}
