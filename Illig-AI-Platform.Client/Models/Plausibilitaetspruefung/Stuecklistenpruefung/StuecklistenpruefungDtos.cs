namespace Illig_AI_Platform.Client.Models.Plausibilitaetspruefung.Stuecklistenpruefung;

// Client-eigene DTOs für die Stücklistenprüfung — bewusst ohne Shared/EF-Abhängigkeit
// (vgl. SondermerkmalDtos.cs). Spiegeln die Shape der Server-DTOs; JSON-(De)Serialisierung
// läuft über die Eigenschaftsnamen.

public record ErkanntesMerkmal(
    string Position,
    string Merkmalsnummer,
    string Beschreibung);

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
    VerlaufDetail? BestehenderEintrag = null,
    bool UmsetzungsmatrixVorhanden = false,
    IReadOnlyList<string>? VerfuegbareUmsetzungsmatrizen = null);

public record VerlaufEintragUebersicht(
    int Id,
    string Dateiname,
    string Auftragsnummer,
    string Maschinentyp,
    DateTime ErstelltAm,
    string? HochgeladenVon);

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

// Client-Spiegel zum Server-Enum AuftragsdokumentQuelle. Reihenfolge (= Zahlenwert) muss zum
// Server passen, da System.Text.Json das Enum als Zahl (de)serialisiert — wie bei VergleichsStatus.
public enum AuftragsdokumentQuelle
{
    DragAndDrop,
    SharePoint
}

public record VerlaufDetail(
    int Id,
    string Dateiname,
    AuftragsdokumentQuelle Quelle,
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
    IReadOnlyList<string>? VerfuegbareUmsetzungsmatrizen = null,
    string? WebUrl = null);

public record StuecklistenKnoten(
    string Artikelnummer,
    string Bezeichnung,
    decimal Menge,
    string Einheit,
    IReadOnlyList<StuecklistenKnoten> Kinder);

public enum VergleichsStatus
{
    Uebereinstimmung,
    Abweichung,
    NurBeiUns,
    NurInSap
}

public record VergleichsPosition(
    string Pfad,
    string Artikelnummer,
    string Bezeichnung,
    string SAPBezeichnung,
    decimal? UnsereMenge,
    string? UnsereEinheit,
    decimal? SapMenge,
    string? SapEinheit,
    VergleichsStatus Status,
    string? Hinweis);

public record VergleichsKnoten(
    string Artikelnummer,
    string Bezeichnung,
    string SAPBezeichnung,
    decimal? UnsereMenge,
    string? UnsereEinheit,
    decimal? SapMenge,
    string? SapEinheit,
    VergleichsStatus Status,
    string? Hinweis,
    IReadOnlyList<VergleichsKnoten> Kinder);

public record VergleichsErgebnis(
    VergleichsKnoten Wurzel,
    IReadOnlyList<VergleichsPosition> NurInSap);
