namespace Illig_AI_Platform.Shared.Kunden;

public record KundenUebersicht(
    int Id,
    string Anzeigename,
    string? Kundennummer,
    string? Adresse,
    KundeStatus Status,
    int Angebote,
    int Kundenbestellungen,
    int Auftraege,
    int Maschinen,
    DateTime LetzteAktivitaet);

public record KundenAuftragPosition(
    string Positionsnummer,
    string VollstaendigeAuftragsnummer,
    string Maschinentyp,
    DateOnly? Datum);

public record KundenAuftrag(
    string Auftragsnummer,
    DateOnly? Datum,
    IReadOnlyList<KundenAuftragPosition> Positionen);

public record KundenDokument(
    KundenQuelltyp Quelltyp,
    int QuellId,
    string Titel,
    string Dateiname,
    DateTime ErfasstAm);

public record KundenDetail(
    int Id,
    string Anzeigename,
    string? Kundennummer,
    string? Adresse,
    KundeStatus Status,
    IReadOnlyList<string> Angebotsnummern,
    IReadOnlyList<string> Kundenbestellnummern,
    IReadOnlyList<KundenAuftrag> Auftraege,
    IReadOnlyList<KundenQuelle> Quellen,
    IReadOnlyList<KundenDokument> Dokumente);
