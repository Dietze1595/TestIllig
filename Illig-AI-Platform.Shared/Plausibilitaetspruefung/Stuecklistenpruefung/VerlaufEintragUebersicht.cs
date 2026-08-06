namespace Illig_AI_Platform.Shared.Plausibilitaetspruefung.Stuecklistenpruefung;

public record VerlaufEintragUebersicht(
    int Id,
    string Dateiname,
    string Auftragsnummer,
    string Maschinentyp,
    DateTime ErstelltAm,
    string? HochgeladenVon);
