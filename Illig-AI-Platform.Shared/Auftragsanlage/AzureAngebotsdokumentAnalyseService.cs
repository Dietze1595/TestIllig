using Azure;
using Azure.AI.DocumentIntelligence;
using System.Text.RegularExpressions;

namespace Illig_AI_Platform.Shared.Auftragsanlage;

/// <summary>
/// Liest den vollständigen Dokumenttext mit dem schlanken Layout-Modell. Die für den
/// Vertriebsprozess benötigten ILLIG-Felder werden anschließend deterministisch aus den
/// beschrifteten Textblöcken gelesen; ein LLM ist hier bewusst nicht beteiligt.
/// </summary>
public class AzureAngebotsdokumentAnalyseService(DocumentIntelligenceClient client) : IAngebotsdokumentAnalyseService
{
    public async Task<ExtrahierteAngebotsdaten> AnalyzeAsync(Stream pdfStream, CancellationToken cancellationToken = default)
    {
        var binaryData = await BinaryData.FromStreamAsync(pdfStream, cancellationToken);
        var operation = await client.AnalyzeDocumentAsync(
            WaitUntil.Completed, "prebuilt-layout", binaryData, cancellationToken: cancellationToken);

        var volltext = operation.Value.Content ?? "";

        // In mehrspaltigen SAP-Bereichen kann der Content-Lesefluss zunächst mehrere
        // Beschriftungen und erst danach deren Werte enthalten. Die Seitenzeilen besitzen
        // Positionsdaten; damit lässt sich der Wert rechts neben "type of transport"
        // eindeutig derselben sichtbaren Zeile zuordnen.
        var versandartAusLayout = ErmittleVersandartAusLayout(operation.Value.Pages);
        if (!string.IsNullOrWhiteSpace(versandartAusLayout))
            volltext = $"type of transport: {versandartAusLayout}\n{volltext}";

        // Pages bzw. Lines können je nach Dokument/SDK-Antwort fehlen oder leer sein. In dem Fall
        // wird auf den vollständigen Dokumenttext zurückgefallen; die Dokumentart-Prüfung
        // (AngebotsdokumentErkennung) nutzt denselben Fallback.
        var ersteSeiteLines = operation.Value.Pages is { Count: > 0 } seiten
            ? seiten[0].Lines
            : null;
        var ersteSeiteText = ersteSeiteLines is { Count: > 0 }
            ? string.Join('\n', ersteSeiteLines.Select(line => line.Content))
            : volltext;

        return AngebotsLayoutParser.Parse(volltext) with { ErsteSeiteText = ersteSeiteText };
    }

    private static string? ErmittleVersandartAusLayout(IReadOnlyList<DocumentPage> seiten)
    {
        foreach (var seite in seiten)
        {
            var zeilen = seite.Lines;
            for (var index = 0; index < zeilen.Count; index++)
            {
                var beschriftung = zeilen[index];
                var direkt = VertriebsbedingungenParser.Parse(beschriftung.Content).Versandbedingung;
                if (!string.IsNullOrWhiteSpace(direkt))
                    return direkt;

                if (!Regex.IsMatch(beschriftung.Content,
                        @"^\s*(?:type\s+of\s+transport|versandart)\s*:?[\s-]*$",
                        RegexOptions.IgnoreCase))
                    continue;

                var polygon = beschriftung.Polygon;
                if (polygon.Count < 4)
                    continue;

                var rechts = MaxX(polygon);
                var mitteY = MitteY(polygon);
                var hoehe = Math.Max(0.08f, MaxY(polygon) - MinY(polygon));

                var kandidat = zeilen
                    .Where((zeile, kandidatIndex) => kandidatIndex != index && zeile.Polygon.Count >= 4)
                    .Select(zeile => new
                    {
                        Zeile = zeile,
                        AbstandY = Math.Abs(MitteY(zeile.Polygon) - mitteY),
                        Links = MinX(zeile.Polygon),
                    })
                    .Where(x => x.AbstandY <= hoehe && x.Links >= rechts - 0.05f)
                    .OrderBy(x => x.AbstandY)
                    .ThenBy(x => x.Links)
                    .Select(x => x.Zeile.Content.Trim())
                    .FirstOrDefault(IstPlausiblerLayoutWert);

                if (kandidat is not null)
                    return kandidat;
            }
        }

        return null;
    }

    private static bool IstPlausiblerLayoutWert(string wert) =>
        !string.IsNullOrWhiteSpace(wert) &&
        wert.Length <= 120 &&
        wert is not "-" and not "–" and not "—" &&
        !Regex.IsMatch(wert, @"^[\p{L}][\p{L}\d\s./_-]{0,40}\s*:$", RegexOptions.IgnoreCase);

    private static float MinX(IReadOnlyList<float> polygon) =>
        Enumerable.Range(0, polygon.Count / 2).Min(index => polygon[index * 2]);

    private static float MaxX(IReadOnlyList<float> polygon) =>
        Enumerable.Range(0, polygon.Count / 2).Max(index => polygon[index * 2]);

    private static float MinY(IReadOnlyList<float> polygon) =>
        Enumerable.Range(0, polygon.Count / 2).Min(index => polygon[index * 2 + 1]);

    private static float MaxY(IReadOnlyList<float> polygon) =>
        Enumerable.Range(0, polygon.Count / 2).Max(index => polygon[index * 2 + 1]);

    private static float MitteY(IReadOnlyList<float> polygon) => (MinY(polygon) + MaxY(polygon)) / 2f;
}
