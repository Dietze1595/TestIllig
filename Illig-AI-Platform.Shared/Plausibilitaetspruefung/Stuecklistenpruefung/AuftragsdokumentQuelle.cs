namespace Illig_AI_Platform.Shared.Plausibilitaetspruefung.Stuecklistenpruefung;

/// <summary>Woher ein <see cref="StuecklistenpruefungVerlaufEintrag"/> stammt.</summary>
public enum AuftragsdokumentQuelle
{
    /// <summary>Manuell durch einen Benutzer hochgeladen (interaktive Stücklistenprüfung).</summary>
    DragAndDrop,

    /// <summary>Automatisch aus dem SharePoint-Ordner synchronisiert.</summary>
    SharePoint
}
