using ClosedXML.Excel;

namespace Illig_AI_Platform.Shared.Plausibilitaetspruefung.Stuecklistenpruefung;

/// <summary>
/// Parser für das <b>RDM73K-Format</b> der Umsetzungsmatrix (Übergangslösung, bis ILLIG eine
/// standardisierte Matrix liefert). Strukturell dem RDM75-Format (<see cref="UmsetzungsmatrixXlsxParser"/>)
/// sehr ähnlich — gleiches "Umsetztabelle"-Blatt, Hierarchie in Spalte B+Tiefe, Varianten ab
/// Spalte N (14), "Merkmalskombination"-Spalte, "siehe Komb."-Verweise. Bewusst als <b>eigene
/// Klasse</b> gehalten (nicht parametrisiert), damit RDM73K-spezifische Anpassungen die anderen
/// Formate nicht berühren. Beobachtete Eigenheiten dieses Formats: mehrzeiliger Kopf (Zeilen 1–6
/// mit Zusatzangaben wie E/P, PLI, Variantenschlüsseln), Nutzdaten ab Zeile 8, "Merkmalskombination"
/// als verbundene Zelle über Zeile 1 und 2. Die Kopfzeilen stehen in Spalten/Zeilen, die die
/// Hierarchie- und Bedingungserkennung ohnehin überspringt.
/// </summary>
public static class UmsetzungsmatrixRdm73kXlsxParser
{
    private const int ErsteHierarchieSpalte = 2; // B
    private const int ErsteVariantenSpalte = 14; // N
    private const int HeaderZeile = 2; // "Merkmalskombination" steht (verbunden) in Zeile 1 und 2.

    public static List<MatrixZeile> Parse(Stream xlsxStream)
    {
        using var wb = new XLWorkbook(xlsxStream);
        var ws = wb.Worksheets.First(w => w.Name.StartsWith("Umsetztabelle", StringComparison.OrdinalIgnoreCase));

        var letzteSpalte = ws.LastColumnUsed()!.ColumnNumber();
        var kombinationsSpalte = FindeKombinationsSpalte(ws, letzteSpalte);

        var pfadStapel = new List<(int Tiefe, string Artikelnummer)>();
        var ergebnis = new List<MatrixZeile>();

        var letzteZeile = ws.LastRowUsed()!.RowNumber();
        for (var zeile = HeaderZeile + 1; zeile <= letzteZeile; zeile++)
        {
            var tiefe = FindeHierarchieTiefe(ws, zeile, letzteSpalte);
            if (tiefe is null)
                continue; // Zeile ohne Artikelnummer (Kopf-, Legenden- oder Leerzeile).

            var artikelnummer = ws.Cell(zeile, ErsteHierarchieSpalte + tiefe.Value).GetString().Trim();

            while (pfadStapel.Count > 0 && pfadStapel[^1].Tiefe >= tiefe.Value)
                pfadStapel.RemoveAt(pfadStapel.Count - 1);
            pfadStapel.Add((tiefe.Value, artikelnummer));

            var bedingung = ErmittleBedingung(ws, zeile, letzteSpalte, kombinationsSpalte);
            if (bedingung is not null)
                ergebnis.Add(new MatrixZeile([.. pfadStapel.Select(p => p.Artikelnummer)], bedingung));
        }

        return ergebnis;
    }

    private static int? FindeHierarchieTiefe(IXLWorksheet ws, int zeile, int letzteSpalte)
    {
        for (var spalte = ErsteHierarchieSpalte; spalte < Math.Min(ErsteVariantenSpalte, letzteSpalte); spalte++)
        {
            var wert = ws.Cell(zeile, spalte).GetString().Trim();
            // Artikelnummern sind rein numerisch — filtert Kopf-/Freitextzeilen (E/P, PLI, Legende) heraus.
            if (wert.Length > 0 && wert.All(char.IsDigit))
                return spalte - ErsteHierarchieSpalte;
        }
        return null;
    }

    private static int FindeKombinationsSpalte(IXLWorksheet ws, int letzteSpalte)
    {
        for (var spalte = ErsteVariantenSpalte; spalte <= letzteSpalte; spalte++)
        {
            if (ws.Cell(HeaderZeile, spalte).GetString().Contains("Merkmalskombination", StringComparison.OrdinalIgnoreCase))
                return spalte;
        }
        return -1; // Keine Kombinationsspalte (z. B. in der Test-Mini-Matrix) — kein Fehler.
    }

    private static string? ErmittleBedingung(IXLWorksheet ws, int zeile, int letzteSpalte, int kombinationsSpalte)
    {
        // Alle gefüllten Varianten-Spalten (außer der Kombinationsspalte selbst) einsammeln.
        var teilbedingungen = new List<string>();
        for (var spalte = ErsteVariantenSpalte; spalte <= letzteSpalte; spalte++)
        {
            if (spalte == kombinationsSpalte)
                continue;

            var wert = ws.Cell(zeile, spalte).GetString().Trim();
            if (wert.Length > 0)
                teilbedingungen.Add(wert);
        }

        if (teilbedingungen.Count == 0)
            return null;

        // Ist die Merkmalskombinationsspalte gefüllt, ist SIE die maßgebliche, vollständige Bedingung
        // der Zeile — die Einzelspalten markieren dann nur die beteiligten Varianten-Gruppen (teils als
        // "siehe Komb."/"s. Kombi"-Verweis, teils mit den Rohwerten). Beobachtet: mal ODER (Zeile 449),
        // mal die per "siehe Komb." verwiesene Kombination.
        if (kombinationsSpalte > 0)
        {
            var kombi = ws.Cell(zeile, kombinationsSpalte).GetString().Trim();
            if (kombi.Length > 0)
                return kombi;
        }

        // Sonst: mehrere gefüllte Spalten gehören per UND zusammen (verschiedene Varianten-Gruppen,
        // z. B. Zeile 317). Früher wurde nur die erste Spalte übernommen und der Rest verworfen.
        return teilbedingungen.Count == 1
            ? teilbedingungen[0]
            : string.Join(" U ", teilbedingungen.Select(t => $"({t})"));
    }
}
