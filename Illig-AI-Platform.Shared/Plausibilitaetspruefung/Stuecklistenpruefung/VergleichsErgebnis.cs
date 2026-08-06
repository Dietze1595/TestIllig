namespace Illig_AI_Platform.Shared.Plausibilitaetspruefung.Stuecklistenpruefung;

public record VergleichsErgebnis(
    VergleichsKnoten Wurzel,
    IReadOnlyList<VergleichsPosition> NurInSap);
