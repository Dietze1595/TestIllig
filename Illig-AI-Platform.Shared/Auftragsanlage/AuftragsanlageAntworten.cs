namespace Illig_AI_Platform.Shared.Auftragsanlage;

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
    bool KundenUndLieferadresseIdentisch = false)
{
    public bool AlleAutomatischenPruefungenBestanden =>
        KundeVorhanden && NummerVorhanden && ZahlungsbedingungenVorhanden &&
        IncotermGueltig && VersandbedingungErkannt && ZahlungsbedingungStandardErkannt &&
        ZahlungsplanStandardErkannt && VerkaeuferVorhanden && LieferterminVorhanden &&
        GueltigkeitsdatumGueltig && KundenUndLieferadresseIdentisch;

    /// <summary>
    /// Berechnet die Checkliste aus den Rohfeldern — beim Vertriebs-Upload mit frisch
    /// extrahierten Daten genutzt, künftig auch beim Innendienst-Abgleich mit den bereits
    /// gespeicherten Angebots-Feldern, damit die Logik nicht doppelt gepflegt werden muss.
    /// </summary>
    public static AngebotsCheckliste Berechnen(
        string? kundenname, string? kundenadresse, string? nummer,
        string? zahlungsbedingungen, string? incoterm, string? incotermOrt, string? versandbedingung,
        string? zahlungsbedingungCode = null, string? zahlungsplan = null, string? verkaeufer = null,
        string? liefertermin = null, DateTime? gueltigBis = null, string? lieferadresse = null)
    {
        var incotermGueltig = incoterm switch
        {
            "FCA" => true,
            "CPT" => !string.IsNullOrWhiteSpace(incotermOrt),
            _ => false,
        };

        return new AngebotsCheckliste(
            KundeVorhanden: !string.IsNullOrWhiteSpace(kundenname) && !string.IsNullOrWhiteSpace(kundenadresse),
            NummerVorhanden: !string.IsNullOrWhiteSpace(nummer),
            ZahlungsbedingungenVorhanden: !string.IsNullOrWhiteSpace(zahlungsbedingungen),
            IncotermGueltig: incotermGueltig,
            VersandbedingungErkannt: IstBekannteVersandkategorie(versandbedingung),
            ZahlungsbedingungStandardErkannt: zahlungsbedingungCode is "A14" or "A30" or "ALC",
            ZahlungsplanStandardErkannt: IstStandardZahlungsplan(zahlungsplan),
            VerkaeuferVorhanden: !string.IsNullOrWhiteSpace(verkaeufer),
            LieferterminVorhanden: !string.IsNullOrWhiteSpace(liefertermin),
            GueltigkeitsdatumGueltig: gueltigBis is not null && gueltigBis.Value.Date >= DateTime.UtcNow.Date,
            KundenUndLieferadresseIdentisch:
                AngebotsadressenPruefung.StimmenUeberein(kundenadresse, lieferadresse));
    }

    private static bool IstStandardZahlungsplan(string? zahlungsplan)
    {
        if (string.IsNullOrWhiteSpace(zahlungsplan))
            return false;

        var prozente = System.Text.RegularExpressions.Regex.Matches(zahlungsplan, @"(?<!\d)(\d{1,3})\s*%")
            .Select(m => int.Parse(m.Groups[1].Value))
            .ToArray();

        // Ohne verlässliche Dokumentklassifikation werden beide fachlichen Standards
        // akzeptiert: Maschinen 30/30/30/10, Werkzeuge 50/50.
        return prozente.SequenceEqual([30, 30, 30, 10]) || prozente.SequenceEqual([50, 50]);
    }

    public static bool IstBekannteVersandkategorie(string? versandbedingung) =>
        !string.IsNullOrWhiteSpace(versandbedingung) &&
        System.Text.RegularExpressions.Regex.IsMatch(
            versandbedingung,
            @"\b(?:pick[\s-]?up|selbstabholung|sea\s*freight|ocean\s+freight|seefracht|air\s*freight|luftfracht|forwarder|road\s+freight|spedition|by\s+truck|truck|lkw|economy)\b",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

}

/// <summary>
/// Ergebnis des Auslese-Schritts (analyse): Checkliste + Rohdaten + erkannte
/// Vertriebsbedingungen + bereits vorhandene Versionen der Angebotsnummer (leer = kein Konflikt).
/// Der Auslese-Schritt schreibt nichts; erst /speichern legt das Angebot ab.
/// </summary>
public record AngebotAnalyseAntwort(
    AngebotsCheckliste Checkliste,
    ExtrahierteAngebotsdaten Daten,
    string? Incoterm,
    string? IncotermOrt,
    string? Versandbedingung,
    IReadOnlyList<int> VorhandeneVersionen);

/// <summary>
/// Ergebnis des Speichern-Schritts. Konflikt kann auftreten, wenn zwischen analyse und
/// speichern (Strategie „Keine") parallel dieselbe Nummer angelegt wurde.
/// </summary>
public record AngebotSpeichernAntwort(
    bool Gespeichert,
    bool Konflikt,
    string? Angebotsnummer,
    int? Version,
    IReadOnlyList<int> VorhandeneVersionen,
    int? Id);

/// <summary>
/// Antwort auf den Freigeben-Endpunkt: Zeitpunkt, zu dem das Angebot freigegeben wurde.
/// </summary>
public record AngebotFreigabeAntwort(DateTime FreigegebenAm);

/// <summary>Body für den Freigeben-Endpunkt: Kommentare zu den vier kommentierbaren Checkliste-Punkten.</summary>
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

/// <summary>
/// Vertriebs-Zustand des dem Bestätigungsabgleich zugeordneten Angebots: Checkliste,
/// Rohwerte, Kommentare und Freigabe-Status — identisch zu dem, was der Vertrieb beim
/// Upload sah, damit der Innendienst weiß, ob und warum das Angebot (nicht) vollständig war.
/// </summary>
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
    string? SapFuehrendKommentar = null,
    string? VersionsKommentar = null);

/// <summary>
/// Antwort auf den Innendienst-Upload. Bei <see cref="AngebotZuordnungStatus.Gefunden"/>
/// sind Angebotsnummer/-version sowie das Vergleichsergebnis gesetzt. Bei
/// <see cref="AngebotZuordnungStatus.Mehrdeutig"/> ist stattdessen
/// <see cref="KandidatenNummern"/> gefüllt. <see cref="AngebotZuordnungStatus.NichtGefunden"/>
/// wird nie über dieses DTO transportiert — dafür liefert der Controller 404 mit Meldungstext.
/// </summary>
public record BestaetigungVergleichAntwort(
    AngebotZuordnungStatus Status,
    string? Angebotsnummer,
    int? AngebotVersion,
    int? AngebotId,
    IReadOnlyList<string> KandidatenNummern,
    string? LieferterminAngebot,
    string? LieferterminBestaetigung,
    bool LieferterminIdentisch,
    IReadOnlyList<string> Uebereinstimmungen,
    IReadOnlyList<string> SonstigeAbweichungen,
    AngebotStatusAntwort? AngebotStatus,
    int? BestaetigungId = null,
    IReadOnlyList<int>? VorhandeneVersionen = null);

public record BestaetigungDetailAntwort(
    int Id,
    string Dateiname,
    BestaetigungVergleichAntwort Vergleich);

public record AngebotUebersicht(
    int Id,
    string Angebotsnummer,
    int Version,
    string? Kundenname,
    string? HochgeladenVon,
    DateTime HochgeladenAm,
    bool Freigegeben);

/// <summary>
/// Vollständiger Zustand eines gespeicherten Angebots für die schreibgeschützte
/// Wiederherstellung in der Vertriebs-Historie: Checkliste (neu berechnet), Rohdaten inkl.
/// Positionen, Vertriebsbedingungen, Kommentare und Freigabe-Status.
/// </summary>
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
    // Owner des Angebots (Uploader). Grundlage für das „nur der Owner darf bearbeiten"-Gate
    // der Vertriebs-Historie. Null bei Altbeständen ohne Nutzerkontext → nicht bearbeitbar.
    Guid? UserProfileId = null,
    string? SapSparteKommentar = null,
    string? SapFuehrendKommentar = null);
