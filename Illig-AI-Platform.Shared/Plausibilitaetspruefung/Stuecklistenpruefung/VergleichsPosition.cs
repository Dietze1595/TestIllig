namespace Illig_AI_Platform.Shared.Plausibilitaetspruefung.Stuecklistenpruefung;

/// <summary>
/// Eine Zeile im Stücklisten-Vergleich. <see cref="Pfad"/> ist die Kette der
/// Vorfahren-Artikelnummern (z. B. "9209307 › DOKU_RDM75KC › …"), damit die flache Tabelle
/// trotzdem erkennen lässt, an welcher Stelle im Baum die Position steht — eine Position
/// gilt nur als "an derselben Stelle" wie ihr Gegenstück, wenn Artikelnummer UND Pfad
/// übereinstimmen. Je nach <see cref="Status"/> sind die "Unsere"- oder "Sap"-Felder null
/// (z. B. bei <see cref="VergleichsStatus.NurInSap"/> gibt es keine "Unsere"-Werte).
/// </summary>
public record VergleichsPosition(
    string Pfad,
    string Artikelnummer,
    string Bezeichnung,
    decimal? UnsereMenge,
    string? UnsereEinheit,
    decimal? SapMenge,
    string? SapEinheit,
    VergleichsStatus Status,
    string? Hinweis);
