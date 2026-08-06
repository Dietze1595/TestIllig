namespace Illig_AI_Platform.Shared.Plausibilitaetspruefung.Sondermerkmalsuche;

/// <summary>
/// Suchanfrage für Step 2→3. Enthält die Merkmalsnummern des Originalauftrags
/// sowie dessen Auftragsnummer, damit der Quellauftrag aus den Treffern ausgeschlossen wird.
/// </summary>
public record SearchRequest(
    string? QuellAuftragsnummer,
    IReadOnlyList<string> Merkmalsnummern);
