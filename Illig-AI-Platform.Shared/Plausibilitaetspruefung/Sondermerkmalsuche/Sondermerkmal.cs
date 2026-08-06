namespace Illig_AI_Platform.Shared.Plausibilitaetspruefung.Sondermerkmalsuche;

/// <summary>
/// Ein aus einer Stückliste erkanntes Sondermerkmal. Position ist der volle Rohwert aus
/// der Auftragsinformation (z. B. "40/330" — Gruppe 40, Unterposition 330), kein
/// geparster int mehr — echte Positionen können ein "/" enthalten, das früher jedes Mal
/// stillschweigend auf 0 kollabierte.
/// </summary>
public record Sondermerkmal(
    string Position,
    string Merkmalsnummer,
    string Beschreibung,
    string Quelle);
