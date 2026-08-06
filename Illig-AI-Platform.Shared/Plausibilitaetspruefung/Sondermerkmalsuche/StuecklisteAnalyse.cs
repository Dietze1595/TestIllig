namespace Illig_AI_Platform.Shared.Plausibilitaetspruefung.Sondermerkmalsuche;

/// <summary>
/// Ergebnis der Auftrags-Analyse (Step 1→2): globale Stücklisten-Infos + erkannte Sondermerkmale.
/// </summary>
public record StuecklisteAnalyse(
    string Auftragsnummer,
    string Maschinentyp,
    DateOnly Datum,
    string Kundennummer,
    string? Kundenname,
    IReadOnlyList<Sondermerkmal> Sondermerkmale);
