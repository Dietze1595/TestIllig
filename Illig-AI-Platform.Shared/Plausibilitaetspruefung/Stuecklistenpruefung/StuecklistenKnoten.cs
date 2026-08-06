namespace Illig_AI_Platform.Shared.Plausibilitaetspruefung.Stuecklistenpruefung;

public record StuecklistenKnoten(
    string Artikelnummer,
    string Bezeichnung,
    decimal Menge,
    string Einheit,
    IReadOnlyList<StuecklistenKnoten> Kinder);
