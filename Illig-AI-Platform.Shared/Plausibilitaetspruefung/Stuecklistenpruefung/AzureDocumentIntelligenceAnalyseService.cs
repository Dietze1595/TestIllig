using Azure;
using Azure.AI.DocumentIntelligence;
using Microsoft.Extensions.Logging;

namespace Illig_AI_Platform.Shared.Plausibilitaetspruefung.Stuecklistenpruefung;

public class AzureDocumentIntelligenceAnalyseService(
    DocumentIntelligenceClient client,
    ILogger<AzureDocumentIntelligenceAnalyseService> logger) : IDocumentAnalyseService
{
    public async Task<DokumentAnalyseErgebnis> AnalyzeAsync(Stream pdfStream, CancellationToken cancellationToken = default)
    {
        var binaryData = await BinaryData.FromStreamAsync(pdfStream, cancellationToken);
        var operation = await client.AnalyzeDocumentAsync(
            WaitUntil.Completed, "prebuilt-layout", binaryData, cancellationToken: cancellationToken);

        var content = operation.Value.Content;
        var seitenLayoutContent = operation.Value.Pages is { Count: > 0 } seiten
            ? string.Join('\n', seiten.SelectMany(seite => seite.Lines).Select(zeile => zeile.Content))
            : null;
        var positionsLayoutContent = AuftragsinformationLayoutParser.ErzeugePositionsText(
            operation.Value.Pages.Select(seite => (IReadOnlyList<AuftragsinformationLayoutZeile>)seite.Lines
                .Select(zeile => new AuftragsinformationLayoutZeile(zeile.Content, zeile.Polygon))
                .ToList()));
        var tabellenLayoutContent = ErzeugeTabellenLayoutContent(operation.Value.Tables);
        var ergebnis = AuftragsinformationParser.Parse(
            content,
            seitenLayoutContent,
            tabellenLayoutContent,
            positionsLayoutContent);

        if (string.IsNullOrWhiteSpace(ergebnis.Auftragsnummer))
        {
            logger.LogWarning(
                "AuftragsinformationParser hat keine Auftragsnummer gefunden (erkannter Inhalt: {ContentLength} Zeichen).",
                content.Length);
        }

        return ergebnis;
    }

    /// <summary>
    /// Der globale Lesetext und selbst die Seitenzeilen koennen bei zweispaltigen Tabellen erst
    /// mehrere Positionen und danach deren Merkmale liefern. Die Tabellenzellen besitzen dagegen
    /// eine stabile Zeilen-/Spaltenzuordnung. Jede Zelle wird deshalb in sichtbarer Tabellenfolge
    /// als eigener Textblock ausgegeben; der Parser kann Position und Merkmalsnummer anschliessend
    /// sowohl getrennt als auch kombiniert verarbeiten.
    /// </summary>
    private static string? ErzeugeTabellenLayoutContent(IReadOnlyList<DocumentTable> tabellen)
    {
        if (tabellen.Count == 0)
            return null;

        var zellen = tabellen
            .SelectMany((tabelle, tabellenIndex) => tabelle.Cells.Select(zelle => new
            {
                TabellenIndex = tabellenIndex,
                zelle.RowIndex,
                zelle.ColumnIndex,
                zelle.Content
            }))
            .OrderBy(zelle => zelle.TabellenIndex)
            .ThenBy(zelle => zelle.RowIndex)
            .ThenBy(zelle => zelle.ColumnIndex)
            .Select(zelle => zelle.Content.Trim())
            .Where(content => content.Length > 0);

        var ergebnis = string.Join('\n', zellen);
        return ergebnis.Length > 0 ? ergebnis : null;
    }
}
