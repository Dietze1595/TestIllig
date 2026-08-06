namespace Illig_AI_Platform.Shared.Plausibilitaetspruefung.Stuecklistenpruefung;

/// <summary>
/// Format-Weiche für den Umsetzungsmatrix-Import: leitet den Stream je nach gewähltem
/// <see cref="StuecklistenImportFormat"/> an den passenden, formatspezifischen Parser weiter.
/// Jedes Format hat bewusst eine eigene Parser-Klasse, damit Anpassungen an einem Format die
/// anderen nicht berühren.
/// </summary>
public static class UmsetzungsmatrixParser
{
    public static List<MatrixZeile> Parse(Stream xlsxStream, StuecklistenImportFormat format) => format switch
    {
        StuecklistenImportFormat.Rdm75Kc => UmsetzungsmatrixXlsxParser.Parse(xlsxStream),
        StuecklistenImportFormat.Rdm73k => UmsetzungsmatrixRdm73kXlsxParser.Parse(xlsxStream),
        StuecklistenImportFormat.Rdm76Kb => UmsetzungsmatrixRdm76kbXlsxParser.Parse(xlsxStream),
        _ => throw new ArgumentOutOfRangeException(
            nameof(format), format, "Unbekanntes Umsetzungsmatrix-Format.")
    };
}
