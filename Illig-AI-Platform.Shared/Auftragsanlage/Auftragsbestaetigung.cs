namespace Illig_AI_Platform.Shared.Auftragsanlage;

/// <summary>
/// Vom Innendienst hochgeladene Kundenbestellung (im Code weiterhin „Auftragsbestätigung"
/// genannt, siehe Spec Update 2026-07-16 zur Terminologie) inkl. Verknüpfung auf das
/// verglichene Angebot und dem persistierten LLM-Vergleichsergebnis.
/// </summary>
public class Auftragsbestaetigung
{
    public int Id { get; set; }
    public int? KundeId { get; set; }
    public int AngebotId { get; set; }
    public string Nummer { get; set; } = "";
    public string? Kundenname { get; set; }
    public string? Kundenadresse { get; set; }
    public string? Zahlungsbedingungen { get; set; }
    public string Volltext { get; set; } = "";
    public string? LieferterminAngebot { get; set; }
    public string? LieferterminBestaetigung { get; set; }
    public bool LieferterminIdentisch { get; set; }
    // Vom LLM gelieferte Liste, als Zeilen zusammengefügt (kein Verlauf-UI, das die
    // Struktur bräuchte — siehe Spec YAGNI).
    public string SonstigeAbweichungen { get; set; } = "";
    public string Dateiname { get; set; } = "";
    public string BlobPfad { get; set; } = "";
    public DateTime HochgeladenAm { get; set; }

    // Wer den Abgleich hochgeladen hat (Azure oid). Null bei Altbeständen / fehlendem Nutzerkontext.
    // Grundlage für die nutzereigene Innendienst-Historie.
    public Guid? UserProfileId { get; set; }
}
