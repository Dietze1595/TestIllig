namespace Illig_AI_Platform.Shared.Plausibilitaetspruefung.Stuecklistenpruefung;

/// <summary>
/// Ein Durchlauf der Stücklistenprüfung, read-only im Nachhinein abrufbar. Bildet sowohl manuell
/// per Drag&amp;Drop hochgeladene Dokumente (<see cref="Quelle"/> = <see cref="AuftragsdokumentQuelle.DragAndDrop"/>)
/// als auch automatisch aus SharePoint synchronisierte Auftragsdokumente
/// (<see cref="Quelle"/> = <see cref="AuftragsdokumentQuelle.SharePoint"/>) ab — beide Workflows
/// lesen dieselbe Dokumentstruktur (Kopf + Merkmale) ein und wurden daher in ein gemeinsames
/// Tabellenmodell konsolidiert.
/// <see cref="UserProfileId"/> hat bewusst keinen DB-FK/Navigation-Property (kein Lösch-Pfad für
/// UserProfile vorhanden; verwaiste Zeilen würden durch die strikte User-Filterung in
/// <c>StuecklistenpruefungVerlaufService</c> ohnehin nur unsichtbar, nicht zu Fehlern führen).
/// Merkmale/Sonderoptionen liegen in <see cref="VerlaufMerkmal"/> (FK
/// <see cref="VerlaufMerkmal.VerlaufEintragId"/>), nicht mehr als JSON — damit die
/// Sondermerkmalsuche nach Merkmalsnummer über alle Aufträge hinweg filtern kann.
/// </summary>
public class StuecklistenpruefungVerlaufEintrag
{
    public int Id { get; set; }
    public AuftragsdokumentQuelle Quelle { get; set; } = AuftragsdokumentQuelle.DragAndDrop;
    public int? KundeId { get; set; }

    // Drag&Drop-spezifisch: nur bei Quelle == DragAndDrop gesetzt.
    public Guid? UserProfileId { get; set; }
    public string? BlobPfad { get; set; }
    public int ErreichterSchritt { get; set; } = 2;
    public string? StuecklisteJson { get; set; }
    public string? VergleichsErgebnisJson { get; set; }
    public string? SapDateiname { get; set; }

    // SharePoint-spezifisch: nur bei Quelle == SharePoint gesetzt. Das Originaldokument bleibt in
    // SharePoint; Drive-/Item-ID sind die stabile Referenz zum erneuten Abruf.
    public string? SharePointDriveId { get; set; }
    public string? SharePointItemId { get; set; }
    public string? ETag { get; set; }
    public string? WebUrl { get; set; }
    public DateTime? SharePointErstelltAm { get; set; }
    public DateTime? SharePointGeaendertAm { get; set; }
    public AuftragsdokumentAnalyseStatus? AnalyseStatus { get; set; }
    public string? AnalyseFehler { get; set; }
    public DateTime? VerarbeitetAm { get; set; }
    public DateTime? GeloeschtAm { get; set; }

    public string Dateiname { get; set; } = "";
    public string Auftragsnummer { get; set; } = "";
    public string Kundennummer { get; set; } = "";
    // Aus dem Dokumentkopf gelesener Kundenname/-adresse (der eigentliche Kunde, nicht ILLIG als
    // Absender). Dient als Herkunftsnachweis am Eintrag und speist den Kundenstamm.
    public string? Kundenname { get; set; }
    public string? Kundenadresse { get; set; }
    public DateOnly? Datum { get; set; }
    public string Maschinentyp { get; set; } = "";
    public DateTime ErstelltAm { get; set; }
}
