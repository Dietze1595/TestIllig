namespace Illig_AI_Platform.Shared.Plausibilitaetspruefung.Stuecklistenpruefung;

public record UmsetzungsmatrixVerfuegbarkeit(
    bool FuerMaschinentypVorhanden,
    IReadOnlyList<string> VorhandeneMaschinentypSchluessel);
