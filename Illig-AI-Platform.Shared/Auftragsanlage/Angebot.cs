namespace Illig_AI_Platform.Shared.Auftragsanlage;

/// <summary>
/// Vom Vertrieb hochgeladenes Angebot. Pro Angebotsnummer kann es mehrere Versionen geben
/// (Konflikt-Entscheidung „neue Version" beim Upload); der Innendienst vergleicht immer
/// gegen die neueste Version.
/// </summary>
public class Angebot
{
    public int Id { get; set; }
    public int? KundeId { get; set; }
    public string Angebotsnummer { get; set; } = "";
    public int Version { get; set; }
    public string? Kundenname { get; set; }
    public string? Kundenadresse { get; set; }
    public string? Lieferadresse { get; set; }
    public string? Zahlungsbedingungen { get; set; }
    public string? ZahlungsbedingungCode { get; set; }
    public string? Zahlungsplan { get; set; }
    public string? Verkaeufer { get; set; }
    public string? Liefertermin { get; set; }
    public DateTime? GueltigBis { get; set; }
    // Voller von Document Intelligence erkannter Text — Grundlage für die Volltextsuche
    // der Angebotsnummer beim Innendienst-Upload und für den LLM-Vergleich.
    public string Volltext { get; set; } = "";
    public string? Incoterm { get; set; }
    public string? IncotermOrt { get; set; }
    public string? Versandbedingung { get; set; }
    public bool? SapSparteBestaetigt { get; set; }
    public bool? SapFuehrendBestaetigt { get; set; }
    // Begründung, wenn die jeweilige SAP-Bestätigung bei der Freigabe nicht per Haken,
    // sondern per Kommentar erfolgt (Haken ODER Kommentar genügt — analog zu den übrigen
    // Prüfpunkten).
    public string? SapSparteKommentar { get; set; }
    public string? SapFuehrendKommentar { get; set; }
    public bool Freigegeben { get; set; }
    public DateTime? FreigegebenAm { get; set; }
    public Guid? FreigegebenVonUserProfileId { get; set; }
    public string? KundeKommentar { get; set; }
    public string? LieferadresseKommentar { get; set; }
    public string? ZahlungsbedingungenKommentar { get; set; }
    public string? IncotermKommentar { get; set; }
    public string? VersandbedingungKommentar { get; set; }
    public string? ZahlungsplanKommentar { get; set; }
    public string? VerkaeuferKommentar { get; set; }
    public string? LieferterminKommentar { get; set; }
    public string? GueltigkeitsdatumKommentar { get; set; }
    public string Dateiname { get; set; } = "";
    public string BlobPfad { get; set; } = "";
    public DateTime HochgeladenAm { get; set; }
    // Wer das Angebot hochgeladen hat (Azure oid). Null bei Altbeständen / fehlendem Nutzerkontext.
    // Grundlage für den „nur meine"-Filter der Vertriebs-Historie.
    public Guid? UserProfileId { get; set; }
}
