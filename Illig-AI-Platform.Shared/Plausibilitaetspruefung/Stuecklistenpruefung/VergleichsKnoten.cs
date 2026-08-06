namespace Illig_AI_Platform.Shared.Plausibilitaetspruefung.Stuecklistenpruefung;

/// <summary>
/// Ein Knoten im Vergleichsbaum — ersetzt die frühere flache Liste mit Pfad-String
/// (<see cref="VergleichsPosition"/>) für den Hauptbaum. <see cref="Kinder"/> codiert die
/// Abstammung direkt, ein separater Pfad-String ist dafür nicht mehr nötig. "Nur in SAP"
/// bleibt bewusst eine separate flache <see cref="VergleichsPosition"/>-Liste (siehe
/// Design-Spec) — dafür wird weiterhin <see cref="VergleichsPosition"/> verwendet.
/// </summary>
public record VergleichsKnoten(
    string Artikelnummer,
    string Bezeichnung,
    decimal? UnsereMenge,
    string? UnsereEinheit,
    decimal? SapMenge,
    string? SapEinheit,
    VergleichsStatus Status,
    string? Hinweis,
    IReadOnlyList<VergleichsKnoten> Kinder);
