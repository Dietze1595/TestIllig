namespace Illig_AI_Platform.Shared.Plausibilitaetspruefung.Stuecklistenpruefung;

public record VerlaufDetail(
    int Id,
    string Auftragsnummer,
    string Kundennummer,
    DateOnly? Datum,
    string Maschinentyp,
    IReadOnlyList<ErkanntesMerkmal> Merkmale,
    IReadOnlyList<ErkanntesMerkmal> Sonderoptionen,
    int ErreichterSchritt,
    StuecklistenKnoten? Stueckliste,
    VergleichsErgebnis? VergleichsErgebnis,
    string? SapDateiname,
    bool UmsetzungsmatrixVorhanden = false,
    IReadOnlyList<string>? VerfuegbareUmsetzungsmatrizen = null);

public record VerlaufStandAktualisieren(
    int Schritt,
    IReadOnlyList<ErkanntesMerkmal> Merkmale,
    IReadOnlyList<ErkanntesMerkmal> Sonderoptionen,
    StuecklistenKnoten? Stueckliste,
    VergleichsErgebnis? VergleichsErgebnis,
    string? SapDateiname);
