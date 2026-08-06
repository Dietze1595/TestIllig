namespace Illig_AI_Platform.Shared.Plausibilitaetspruefung.Stuecklistenpruefung;

public record DokumentAnalyseErgebnis(
    string Auftragsnummer,
    string Kundennummer,
    DateOnly? Datum,
    string Maschinentyp,
    IReadOnlyList<ErkanntesMerkmal> Merkmale,
    IReadOnlyList<ErkanntesMerkmal> Sonderoptionen,
    string? Kundenname = null,
    string? Kundenadresse = null,
    bool IstAuftragsinformation = true,
    int? VerlaufId = null,
    // Nur gesetzt, wenn dieser Upload ein bereits vom aktuellen User angelegtes Auftragsinformation-
    // Dokument (gleiche Auftragsnr. + Kundennr.) trifft: Der Client lässt zwischen dem gespeicherten
    // Stand und einem vollständigen Neuauslesen des hochgeladenen Dokuments wählen.
    VerlaufDetail? BestehenderEintrag = null,
    bool UmsetzungsmatrixVorhanden = false,
    IReadOnlyList<string>? VerfuegbareUmsetzungsmatrizen = null);
