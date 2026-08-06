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
        // Bis letzteSpalte (nicht bis kombinationsSpalte) laufen — sonst wird die Bedingung nie
        // gefunden, wenn keine Kombinationsspalte existiert (kombinationsSpalte == -1).
        for (var spalte = ErsteVariantenSpalte; spalte <= letzteSpalte; spalte++)
        {
            if (spalte == kombinationsSpalte)
                continue;

            var wert = ws.Cell(zeile, spalte).GetString().Trim();
            if (wert.Length == 0)
                continue;

            if (wert.Contains("siehe Komb.", StringComparison.OrdinalIgnoreCase))
                return kombinationsSpalte > 0 ? ws.Cell(zeile, kombinationsSpalte).GetString().Trim() : wert;

            return wert;
        }
        return null;
    }
}
