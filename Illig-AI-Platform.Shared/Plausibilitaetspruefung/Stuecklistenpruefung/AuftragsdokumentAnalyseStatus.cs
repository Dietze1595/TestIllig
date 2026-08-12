namespace Illig_AI_Platform.Shared.Plausibilitaetspruefung.Stuecklistenpruefung;

/// <summary>Analyse-Status eines per SharePoint synchronisierten Auftragsdokuments.</summary>
public enum AuftragsdokumentAnalyseStatus
{
    Ausstehend = 0,
    InBearbeitung = 1,
    Erfolgreich = 2,
    Fehlgeschlagen = 3,
    Ignoriert = 4,
}
