using Illig_AI_Platform.Shared.Plausibilitaetspruefung.Stuecklistenpruefung;

namespace Illig_AI_Platform.Shared.Auftragsinformationen;

public record AuftragsdokumentUebersicht(
    int Id,
    string Dateiname,
    string Auftragsnummer,
    string Kundennummer,
    string Maschinentyp,
    DateOnly? Datum,
    DateTime SharePointGeaendertAm);

public record AuftragsdokumentDetail(
    int Id,
    string Dateiname,
    string WebUrl,
    string Auftragsnummer,
    string Kundennummer,
    string? Kundenname,
    string? Kundenadresse,
    DateOnly? Datum,
    string Maschinentyp,
    IReadOnlyList<ErkanntesMerkmal> Merkmale,
    IReadOnlyList<ErkanntesMerkmal> Sonderoptionen);
