namespace Illig_AI_Platform.Shared.Plausibilitaetspruefung.Sondermerkmalsuche;

/// <summary>Eine Referenz-Stückliste mit Überschneidung zum Originalauftrag (Step 3).</summary>
public record ReferenzTreffer(
    string Auftragsnummer,
    string Maschinentyp,
    DateOnly Abschlussdatum,
    IReadOnlyList<string> UeberschneidendeMerkmalsnummern,
    int TrefferAnzahl,
    int Gesamtanzahl,
    IReadOnlyList<string> AktuellsteMerkmalsnummern,
    string Kundennummer = "",
    string? Kundenname = null);
