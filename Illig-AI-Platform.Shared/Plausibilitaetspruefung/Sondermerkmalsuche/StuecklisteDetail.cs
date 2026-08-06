namespace Illig_AI_Platform.Shared.Plausibilitaetspruefung.Sondermerkmalsuche;

public record StuecklisteDetailMerkmal(
    string Merkmalsnummer,
    string Beschreibung,
    string Referenzauftrag,
    string Quelle);

/// <summary>Detailansicht einer Referenz-Stückliste (Drawer in Step 3).</summary>
public record StuecklisteDetail(
    string Auftragsnummer,
    string Maschinentyp,
    DateOnly Abschlussdatum,
    string? Kundenname,
    IReadOnlyList<StuecklisteDetailMerkmal> Merkmale);
