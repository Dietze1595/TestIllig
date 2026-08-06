namespace Illig_AI_Platform.Client.Models.Auftragsanlage;

// Spiegel der Server-Records in Illig-AI-Platform.Shared/Auftragsanlage/ —
// der WASM-Client referenziert Shared bewusst nicht (kein EF im Bundle).

public record ExtrahierteAngebotsdaten(
    string? Nummer,
    string? Kundenname,
    string? Kundenadresse,
    string? Zahlungsbedingungen,
    List<ExtrahiertePosition> Positionen,
    // Muss zum Server-Record passen (wird beim Speichern zurückgereicht). Default "" für
    // Aufrufe, die den Volltext nicht setzen.
    string Volltext = "",
    string? ZahlungsbedingungCode = null,
    string? Zahlungsplan = null,
    string? Verkaeufer = null,
    string? Liefertermin = null,
    DateTime? GueltigBis = null,
    string? Gesamtpreis = null,
    string ErsteSeiteText = "",
    string? Versandart = null,
    string? Lieferadresse = null);

public record ExtrahiertePosition(
    string Beschreibung,
    decimal? Menge,
    decimal? Einzelpreis,
    decimal? Gesamtbetrag);

public record AngebotsCheckliste(
    bool KundeVorhanden,
    bool NummerVorhanden,
    bool ZahlungsbedingungenVorhanden,
    bool IncotermGueltig,
    bool VersandbedingungErkannt,
    bool ZahlungsbedingungStandardErkannt,
    bool ZahlungsplanStandardErkannt,
    bool VerkaeuferVorhanden,
    bool LieferterminVorhanden,
    bool GueltigkeitsdatumGueltig,
    bool KundenUndLieferadresseIdentisch = false);

public record AngebotAnalyseAntwort(
    AngebotsCheckliste Checkliste,
    ExtrahierteAngebotsdaten Daten,
    string? Incoterm,
    string? IncotermOrt,
    string? Versandbedingung,
    List<int> VorhandeneVersionen);

public record AngebotSpeichernAntwort(
    bool Gespeichert,
    bool Konflikt,
    string? Angebotsnummer,
    int? Version,
    List<int> VorhandeneVersionen,
    int? Id);

public record AngebotFreigabeAntwort(DateTime FreigegebenAm);

public record AngebotFreigebenAnfrage(
    string? KundeKommentar,
    string? ZahlungsbedingungenKommentar,
    string? IncotermKommentar,
    string? VersandbedingungKommentar,
    string? ZahlungsplanKommentar = null,
    string? VerkaeuferKommentar = null,
    string? LieferterminKommentar = null,
    string? GueltigkeitsdatumKommentar = null,
    bool SapFuehrendBestaetigt = false,
    bool SapSparteBestaetigt = false,
    string? LieferadresseKommentar = null,
    string? SapSparteKommentar = null,
    string? SapFuehrendKommentar = null);

// Reihenfolge muss dem Server-Enum entsprechen (Serialisierung als Zahl).
public enum AngebotZuordnungStatus
{
    Gefunden,
    NichtGefunden,
    Mehrdeutig,
}

public record AngebotStatusAntwort(
    AngebotsCheckliste Checkliste,
    string? Kundenname,
    string? Kundenadresse,
    string? Zahlungsbedingungen,
    string? Incoterm,
    string? IncotermOrt,
    string? Versandbedingung,
    string? ZahlungsbedingungCode,
    string? Zahlungsplan,
    string? Verkaeufer,
    string? Liefertermin,
    DateTime? GueltigBis,
    string? KundeKommentar,
    string? ZahlungsbedingungenKommentar,
    string? IncotermKommentar,
    string? VersandbedingungKommentar,
    string? ZahlungsplanKommentar,
    string? VerkaeuferKommentar,
    string? LieferterminKommentar,
    string? GueltigkeitsdatumKommentar,
    bool Freigegeben,
    DateTime? FreigegebenAm,
    string? FreigegebenVon,
    bool? SapSparteBestaetigt,
    bool? SapFuehrendBestaetigt,
    string? Lieferadresse = null,
    string? LieferadresseKommentar = null,
    string? SapSparteKommentar = null,
    string? SapFuehrendKommentar = null);

public record BestaetigungVergleichAntwort(
    AngebotZuordnungStatus Status,
    string? Angebotsnummer,
    int? AngebotVersion,
    int? AngebotId,
    List<string> KandidatenNummern,
    string? LieferterminAngebot,
    string? LieferterminBestaetigung,
    bool LieferterminIdentisch,
    List<string> Uebereinstimmungen,
    List<string> SonstigeAbweichungen,
    AngebotStatusAntwort? AngebotStatus);

public record BestaetigungDetailAntwort(
    int Id,
    string Dateiname,
    BestaetigungVergleichAntwort Vergleich);

/// <summary>Client-seitige Hülle: 404 („kein Angebot gefunden") wird als Meldung transportiert statt als Exception.</summary>
public record BestaetigungErgebnis(BestaetigungVergleichAntwort? Antwort, string? HinweisMeldung);

public record AngebotUebersicht(
    int Id,
    string Angebotsnummer,
    int Version,
    string? Kundenname,
    string? HochgeladenVon,
    DateTime HochgeladenAm,
    bool Freigegeben);

public record AngebotDetailAntwort(
    int Id,
    string Angebotsnummer,
    int Version,
    string Dateiname,
    AngebotsCheckliste Checkliste,
    ExtrahierteAngebotsdaten Daten,
    string? Incoterm,
    string? IncotermOrt,
    string? Versandbedingung,
    string? KundeKommentar,
    string? ZahlungsbedingungenKommentar,
    string? IncotermKommentar,
    string? VersandbedingungKommentar,
    string? ZahlungsplanKommentar,
    string? VerkaeuferKommentar,
    string? LieferterminKommentar,
    string? GueltigkeitsdatumKommentar,
    bool Freigegeben,
    DateTime? FreigegebenAm,
    bool? SapSparteBestaetigt,
    bool? SapFuehrendBestaetigt,
    string? LieferadresseKommentar = null,
    // Owner des Angebots (Uploader) — Client-Gate für die Bearbeitung in der Historie.
    Guid? UserProfileId = null,
    string? SapSparteKommentar = null,
    string? SapFuehrendKommentar = null);
