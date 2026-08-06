namespace Illig_AI_Platform.Shared.Plausibilitaetspruefung.Stuecklistenpruefung;

/// <summary>
/// Ein Durchlauf der Stücklistenprüfung, read-only im Nachhinein abrufbar.
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
    public int? KundeId { get; set; }
    public Guid UserProfileId { get; set; }
    public string Dateiname { get; set; } = "";
    public string BlobPfad { get; set; } = "";
    public string Auftragsnummer { get; set; } = "";
    public string Kundennummer { get; set; } = "";
    // Aus dem Dokumentkopf gelesener Kundenname/-adresse (der eigentliche Kunde, nicht ILLIG als
    // Absender). Dient als Herkunftsnachweis am Eintrag und speist den Kundenstamm.
    public string? Kundenname { get; set; }
    public string? Kundenadresse { get; set; }
    public DateOnly? Datum { get; set; }
    public string Maschinentyp { get; set; } = "";
    public DateTime ErstelltAm { get; set; }
    public int ErreichterSchritt { get; set; } = 2;
    public string? StuecklisteJson { get; set; }
    public string? VergleichsErgebnisJson { get; set; }
    public string? SapDateiname { get; set; }
}
