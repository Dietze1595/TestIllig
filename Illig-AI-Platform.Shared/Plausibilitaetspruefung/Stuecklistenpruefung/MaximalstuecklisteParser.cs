namespace Illig_AI_Platform.Shared.Plausibilitaetspruefung.Stuecklistenpruefung;

/// <summary>
/// Format-Weiche für die Maximalstückliste (.txt): leitet den Inhalt je nach gewähltem
/// <see cref="StuecklistenImportFormat"/> an den passenden, formatspezifischen Parser weiter.
/// Jedes Format hat bewusst eine eigene Parser-Klasse, damit Anpassungen an einem Format die
/// anderen nicht berühren.
/// </summary>
public static class MaximalstuecklisteParser
{
    public static RohPosition Parse(string inhalt, StuecklistenImportFormat format) => format switch
    {
        StuecklistenImportFormat.Rdm75Kc => MaximalstuecklisteTxtParser.Parse(inhalt),
        StuecklistenImportFormat.Rdm73k => MaximalstuecklisteRdm73kTxtParser.Parse(inhalt),
        StuecklistenImportFormat.Rdm76Kb => MaximalstuecklisteRdm76kbTxtParser.Parse(inhalt),
        _ => throw new ArgumentOutOfRangeException(
            nameof(format), format, "Unbekanntes Stücklisten-Importformat.")
    };
}
