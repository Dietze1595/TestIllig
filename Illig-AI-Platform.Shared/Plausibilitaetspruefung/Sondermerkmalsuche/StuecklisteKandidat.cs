namespace Illig_AI_Platform.Shared.Plausibilitaetspruefung.Sondermerkmalsuche;

/// <summary>Öffentliche Eingabe für den Matcher: eine Stückliste reduziert auf das fürs Ranking Nötige.</summary>
public record StuecklisteKandidat(
    string Auftragsnummer,
    string Maschinentyp,
    DateOnly Abschlussdatum,
    IReadOnlyList<string> Merkmalsnummern,
    string Kundennummer = "",
    string? Kundenname = null);
