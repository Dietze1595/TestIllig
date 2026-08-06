using ClosedXML.Excel;

namespace Illig_AI_Platform.Shared.Plausibilitaetspruefung.Stuecklistenpruefung;

/// <summary>
/// Parser für das <b>RDM76Kb-Format</b> der Umsetzungsmatrix (Übergangslösung). Gegenüber
/// RDM75 (<see cref="UmsetzungsmatrixXlsxParser"/>) und RDM73K
/// (<see cref="UmsetzungsmatrixRdm73kXlsxParser"/>) abweichend:
/// <list type="bullet">
/// <item>Varianten-Spalten beginnen ab Spalte 13 (M) statt 14; Varianten-Kopf in Zeile 3,
/// "Merkmalskombination" ebenfalls in Zeile 3.</item>
/// <item>Hierarchie nur bis Spalte 9 (Artikelnummern in B–G, max. Tiefe 5). Die Spalten 10–12
/// tragen Z-Zeichnung, Materialnummer (Spalte 11 = reine Zahlen!) und Baugruppenkenner und
/// dürfen NICHT als Hierarchie gelesen werden — sonst entstünden hunderte Geister-Knoten.</item>
/// <item><b>Durchgestrichene</b> Artikelnummern sind ersetzte Alt-Nummern und werden bei der
/// Hierarchie-Erkennung übersprungen; gültig ist die nicht durchgestrichene Nummer daneben.</item>
/// </list>
/// Bewusst eigene Klasse (nicht parametrisiert), damit Anpassungen die anderen Formate nicht berühren.
/// </summary>
public static class UmsetzungsmatrixRdm76kbXlsxParser
{
    private const int ErsteHierarchieSpalte = 2;  // B
    private const int LetzteHierarchieSpalte = 9;  // I — davor endet die Hierarchie (Spalte 10+ = Metadaten)
    private const int ErsteVariantenSpalte = 13;   // M
    private const int HeaderZeile = 3;

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
            var tiefe = FindeHierarchieTiefe(ws, zeile);
            if (tiefe is null)
                continue; // Zeile ohne (gültige) Artikelnummer.

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

    private static int? FindeHierarchieTiefe(IXLWorksheet ws, int zeile)
    {
        for (var spalte = ErsteHierarchieSpalte; spalte <= LetzteHierarchieSpalte; spalte++)
        {
            var zelle = ws.Cell(zeile, spalte);
            // Durchgestrichene Zellen sind ersetzte Alt-Nummern — überspringen, damit die gültige
            // Artikelnummer daneben die Hierarchie-Tiefe bestimmt.
            if (zelle.Style.Font.Strikethrough)
                continue;

            var wert = zelle.GetString().Trim();
            // Artikelnummern sind rein numerisch — filtert Freitext-/Kennerspalten heraus.
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
