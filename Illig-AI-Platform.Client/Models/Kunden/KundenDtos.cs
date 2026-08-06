namespace Illig_AI_Platform.Client.Models.Kunden;

public enum KundeStatus
{
    Vorlaeufig = 0,
    Bestaetigt = 1,
    Neukunde = 2,
}

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

public enum KundenQuelltyp
{
    Angebot = 1,
    Kundenbestellung = 2,
    Auftragsinformation = 3,
}

public record KundenQuelle(
    int Id,
    int KundeId,
    KundenQuelltyp Quelltyp,
    int QuellId,
    string? Kundennummer,
    string? Kundenname,
    string? Kundenadresse,
    DateTime ErfasstAm);

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
